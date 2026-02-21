using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Functional.Logger;
using Functional.Util;
using KWeb.BaseServices;
using KWeb.BaseServices.Impl;
using KWeb.Common;
using KWeb.DependencyInjection;
using KWeb.ExceptionHandle;
using KWeb.Expansion;
using KWeb.HttpOption;
using KWeb.HttpOption.HttpOptionBuliderException;
using IServiceProvider = KWeb.DependencyInjection.IServiceProvider;

namespace KWeb;

public class WebApplication
{
    public HttpListener Server { get; }
    public IServiceProvider Services { get; }
    private readonly string[] args;
    private bool isRunning;
    private readonly WebApplication instance = null;
    private FrozenDictionary<(string, string), MethodInfo> webPaths;
    private readonly IEnumerable<Type> entryAssemblyTypes;

    public WebApplication(IServiceProvider services, string[] args)
    {
        if (instance == null)
        {
            instance = this;
            entryAssemblyTypes = Assembly.GetEntryAssembly().GetTypes();
            Server = new HttpListener();
            Services = services;
            this.args = args;
            return;
        }

        throw new SystemException("只能创建一个Web应用！");
    }
    
    public void Run()
    {
        if (isRunning) return;
        isRunning = true;
        Server.Start();
        IConfiguration configuration = Services.Get<IConfiguration>();
        IKLogger<WebApplication> logger = Services.Get<IKLogger<WebApplication>>();
        var urls = configuration["Server"]["Urls"].Get<List<string>>();
        urls.ForEach(url => Server.Prefixes.Add(url + '/'));
        /*int maxProcessCount = Environment.ProcessorCount;
        ThreadPool.SetMaxThreads(maxProcessCount, maxProcessCount);*/
        this.UseDefaultLogger();
        Server.Start();
        while (isRunning)
        {
            WorkMethod();
        }

        Server.Stop();
        Server.Close();
        Services.ReleasingObjs();
        isRunning = false;
    }

    private void WorkMethod(object? _ = null)
    {
        var context = Server.GetContext();
        if (context is not null)
        {
            var request = new HttpRequest(context.Request);
            var response = new HttpResponse(context.Response);
            try
            {
                HandleRequest(request, response);
                response.ResponseTo(context.Response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理请求时出错: {ex}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                if (Services.HasInjected<GlobalExceptionHandler>(Constants.GlobalExceptionHandler))
                {
                    var handler = Services.Get<GlobalExceptionHandler>(Constants.GlobalExceptionHandler);
                    handler.Handle(ex);
                }
            }
            finally
            {
                context.Response.Close();
            }
        }
        //Thread.Sleep(10);
    }

    public void AutoConfig() => this.AddAutoConfiguration(entryAssemblyTypes);

    public void RegisterAllWebPaths()
    {
        var controllerTypes = entryAssemblyTypes
            .Where(type => type.IsSubclassOf(typeof(ControllerBase))
                           && type.GetCustomAttribute<RouteAttribute>() != null
                           && type.GetCustomAttribute<ServiceInjection>() == null);
        var data = new Dictionary<(string, string), MethodInfo>();
        foreach (var controllerType in controllerTypes)
        {
            var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
            string basePath = routeAttribute.Path;
            var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var getMethods = methods.Where(m => m.GetCustomAttribute<HttpGetAttribute>() != null);
            var postMethods = methods.Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null);
            var putMethods = methods.Where(m => m.GetCustomAttribute<HttpPutAttribute>() != null);
            var deleteMethods = methods.Where(m => m.GetCustomAttribute<HttpDeleteAttribute>() != null);
            var patchMethods = methods.Where(m => m.GetCustomAttribute<HttpPatchAttribute>() != null);
            var headMethods = methods.Where(m => m.GetCustomAttribute<HttpHeadAttribute>() != null);
            RegisterWebPath(basePath, getMethods, "GET", data);
            RegisterWebPath(basePath, postMethods, "POST", data);
            RegisterWebPath(basePath, putMethods, "PUT", data);
            RegisterWebPath(basePath, deleteMethods, "DELETE", data);
            RegisterWebPath(basePath, headMethods, "HEAD", data);
            RegisterWebPath(basePath, patchMethods, "PATCH", data);
        }

        webPaths = data.ToFrozenDictionary();
        Register(controllerTypes);
    }

    private void RegisterWebPath(string baseUrl, IEnumerable<MethodInfo> methods, string httpMethod
        , Dictionary<(string, string), MethodInfo> data)
    {
        if (string.IsNullOrEmpty(baseUrl))
            return;
        foreach (var method in methods)
        {
            var route = method.GetCustomAttribute<RouteAttribute>();
            var path = string.IsNullOrEmpty(route.Path) ? '/' + baseUrl : $"/{baseUrl}/{route.Path}";
            if (data.ContainsKey((path, httpMethod)))
                throw new RouteRepeateException();
            data.Add((path, httpMethod), method);
        }
    }

    private void Register(IEnumerable<Type> types)
    {
        List<Type> constructInjectionsTypes = new List<Type>();
        object? instance = null;
        foreach (Type type in types)
        {
            ConstructorInfo? constructor = type.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length > 0);
            if (constructor != null)
            {
                constructInjectionsTypes.Add(type);
                continue;
            }

            ServiceInjection attribute = type.GetCustomAttribute<ServiceInjection>();
            string serviceName = attribute == null ? type.Name : GetServiceName(type, attribute);
            var value = Activator.CreateInstance(type);
            if (type.IsSubclassOf(typeof(ControllerBase)) || attribute.Type == InjectionType.Scoped)
                Services.AddService(type, _ => value ,serviceName);
            else if (attribute.Type == InjectionType.Single)
                Services.AddSingle(type, _ => value, serviceName);
        }


        foreach (Type type in constructInjectionsTypes)
        {
            ConstructorInfo constructor = type.GetConstructors()
                .First(c => c.GetParameters().Length > 0);
            ParameterInfo[] parameters = constructor.GetParameters();
            object[] values = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ServiceInjection? paramAttribute = parameters[i].GetCustomAttribute<ServiceInjection>();
                if (paramAttribute == null)

                {
                    object? value = Services.Get(parameters[i].ParameterType);
                    values[i] = HandleValue(value, type);
                }
                else
                    values[i] = HandleValue(Services.Get(parameters[i].ParameterType, paramAttribute.Name),type);
            }

            ServiceInjection attribute = type.GetCustomAttribute<ServiceInjection>();
            string serviceName = attribute == null ? type.Name : GetServiceName(type, attribute);
            if (type.IsSubclassOf(typeof(ControllerBase)) || attribute.Type == InjectionType.Scoped)
                Services.AddService(_ => constructor.Invoke(values), serviceName);
            else if (attribute.Type == InjectionType.Single)
                Services.AddSingle(_ =>  constructor.Invoke(values), serviceName);
        }
    }

    public void HandleInjectionServices()
    {
        var typesWithInjectionAttr = entryAssemblyTypes
            .Where(type => type.GetCustomAttribute<ServiceInjection>() != null);
        Register(typesWithInjectionAttr);
    }

    private string GetServiceName(Type type, ServiceInjection attribute)
    {
        return attribute.Name == "" ? type.Name : attribute.Name;
    }

    private void HandleRequest(HttpRequest request, HttpResponse response)
    {
        if (webPaths.Keys.Any(e => e.Item1 == request.Path && e.Item2 != request.Method))
        {
            response.StatusCode = HttpStatusCode.MethodNotAllowed;
            response.Result = $"405 {request.Method} is not allowed!";
            return;
        }
        
        if (!webPaths.TryGetValue((request.Path, request.Method), out MethodInfo method))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Result = "404 Not Found";
            return;
        }
        var type = method.ReflectedType;
        if(!Intercepting(type,method,request,response))return;
        if (!Intercepted(type, method, request, response)) return;

        var routeAttribute = method.GetCustomAttribute<RouteAttribute>();
        var controller = Services.Get(type);
        var httpRequestField = type.GetField(nameof(request), BindingFlags.Instance | BindingFlags.NonPublic);
        var httpResponseField = type.GetField(nameof(response), BindingFlags.Instance | BindingFlags.NonPublic);
        httpRequestField.SetValue(controller, request);
        httpResponseField.SetValue(controller, response);
        var parameters = method.GetParameters();
        var values = new object[parameters.Length];
        string pattern = "^" + Regex.Replace(
            Regex.Escape(routeAttribute.Path),
            @"\\\{[^}]+\\\}",
            @"([^/]+)"
        ) + "$";
        Dictionary<string, string> routeParams = new Dictionary<string, string>();
        if (Regex.IsMatch(request.Path, pattern))
        {
            var routeParts = routeAttribute.Path.Split('/');
            var actualRouteParts = request.Path.Split('/');
            for (int i = 0; i < routeParts.Length; i++)
            {
                if (Regex.IsMatch("\\{([^}]+)\\}", routeParts[i]))
                    routeParams.Add(routeParts[i].Replace("{", string.Empty)
                        .Replace("}", string.Empty), actualRouteParts[i]);
            }
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var paramType = parameter.ParameterType;
            var injectionAttr = paramType.GetCustomAttribute<ServiceInjection>();
            var requestBody = request.Body;
            if (injectionAttr != null)
            {
                values[i] = Services.Get(paramType, injectionAttr.Name);
                continue;
            }

            var routeParamAttr = paramType.GetCustomAttribute<RouteParamAttribute>();
            if (routeParamAttr != null)
            {
                if (routeParams.ContainsKey(parameter.Name))
                    values[i] = routeParams[parameter.Name];
                else
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Result = $"(路径参数)route param \'{parameter.Name}\' is required!（必要的!）";
                    return;
                }
            }

            if (paramType.GetCustomAttribute<QueryParamAttribute>() != null)
            {
                if (!request.Query.ContainsKey(parameter.Name))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Result = $"(查询参数)query param \'{parameter.Name}\' is required!（必要的!）";
                    return;
                }
                values[i] = TransformValue(paramType, request.Query[parameter.Name]);
            }
            if (paramType.GetCustomAttribute<RequestBodyAttribute>() != null)
            {
                if(request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH")
                   values[i] = JsonSerializer.Serialize(requestBody, paramType);
                else
                {
                    response.StatusCode = HttpStatusCode.NotAcceptable;
                    response.Result = "406 Not Acceptable!";
                    return;
                }
            }
        }
        
        bool isAsync = method.ReturnType == typeof(Task<>);
        object result;
        if (!isAsync)
            result = method.Invoke(controller, values.Length == 0 ? null : values);
        else
        {
            var task = method.Invoke(controller, values.Length == 0 ? null : values);
            result = task.GetType().GetProperty("Result").GetValue(task);
        }

        if (IsBaseType(result.GetType()))
            response.Headers["Content-Type"] = "text/html;charset=utf-8";
        else
            response.Headers["Content-Type"] = "application/json;charset=utf-8";
        //response.StatusCode = HttpStatusCode.OK;
        CheckCors(type,method,request,response);
        response.Result = JsonSerializer.Serialize(result,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
    }

    private object TransformValue(Type paramType, string paramValue)
    {
        if (paramType == typeof(int) || paramType == typeof(short))
            return int.Parse(paramValue);
        if (paramType == typeof(float))
            return float.Parse(paramValue);
        if (paramType == typeof(double))
            return double.Parse(paramValue);
        if (paramType == typeof(long))
            return long.Parse(paramValue);
        if (paramType == typeof(bool))
            return bool.Parse(paramValue);
        if (paramType == typeof(DateTime))
            return DateTime.Parse(paramValue);
        if (paramType == typeof(DateOnly))
            return DateOnly.Parse(paramValue);
        if (paramType == typeof(decimal))
            return decimal.Parse(paramValue);
        if (paramType == typeof(uint) || paramType == typeof(ushort))
            return uint.Parse(paramValue);
        if (paramType == typeof(ulong))
            return ulong.Parse(paramValue);
        if (paramType == typeof(Guid))
            return Guid.Parse(paramValue);
        return paramValue;
    }

    private bool IsBaseType(Type type)
    {
        return type == typeof(string) ||
               type == typeof(int) || type == typeof(long)
               || type == typeof(float) || type == typeof(double)
               || type == typeof(uint) || type == typeof(ulong);
    }
    
    private object HandleValue(object value,Type injectTo)
    {
        var type = value.GetType();
        if(!type.IsGenericType)return value;
        var genericType = type.GetGenericTypeDefinition();
        var afterType = genericType.MakeGenericType(injectTo);
        var constructor = afterType.GetConstructors().First();
        object newInstance;
        var parameters = constructor.GetParameters();
        if (parameters.Length > 0)
        {
            List<object> parameterValues = [];
            foreach (var parameter in parameters)
            {
                if (!parameter.HasDefaultValue)
                    throw new Exception();
                parameterValues.Add(parameter.DefaultValue);
            }
            newInstance = constructor.Invoke(parameterValues.ToArray());
        }
        else
            newInstance = constructor.Invoke([]);
        newInstance.CopyFields(value);
        newInstance.CopyProperties(value);
        return newInstance;
    }

    private void CheckCors(Type type,MethodInfo method, HttpRequest request,HttpResponse response)
    {
        if (Services.HasInjected(typeof(bool), Constants.CorsServiceName))
        {
            var withCorsMain = type.GetCustomAttribute<WithCorsAttribute>();
            if (withCorsMain != null)
            {
                var corsVerifier = Services.Get<HttpCorsVerifier>(string.IsNullOrEmpty(withCorsMain.Name)?
                    Constants.DefaultCors:withCorsMain.Name);
                if(corsVerifier!=null && corsVerifier.Verify(request))
                    corsVerifier.AddCorsHeaders(response);
            }
            var withCorsSingle = method.GetCustomAttribute<WithCorsAttribute>();
            if (withCorsSingle != null)
            {
                var corsVerifier = Services.Get<HttpCorsVerifier>(string.IsNullOrEmpty(withCorsSingle.Name)?
                    Constants.DefaultCors:withCorsSingle.Name);
                if(corsVerifier != null && corsVerifier.Verify(request))
                    corsVerifier.AddCorsHeaders(response);
            }

            if (withCorsMain == null && withCorsSingle == null)
            {
                var corsVerifier = Services.Get<HttpCorsVerifier>(Constants.DefaultCors);
                if(corsVerifier!=null && corsVerifier.Verify(request))
                    corsVerifier.AddCorsHeaders(response);
            }
        }
    }

    private bool Intercepting(Type type,MethodInfo method,HttpRequest request,HttpResponse response)
    {
        bool res = true;
        if (Services.HasInjected<HttpRequestInterceptor>(Constants.InterceptorServiceName))
        {
            HttpRequestInterceptor interceptor = Services.Get<HttpRequestInterceptor>();

        }

        return res;
    }

    private bool Intercepted(Type type,MethodInfo method,HttpRequest request,HttpResponse response)
    {


        return true;
    }

    private bool CheckInterceptorPatterns(HttpRequest request,HttpResponse response,HttpRequestInterceptor interceptor)
    {
        foreach (var pattern in interceptor.ExcludedPatterns)
        {
            var regex = new Regex(pattern);
            if (regex.IsMatch(request.Path))
                return true;
        }
        foreach (var pattern in interceptor.IncludedPatterns)
        {
            var regex = new Regex(pattern);
            if (regex.IsMatch(request.Path))
                return interceptor.ToHandle(request,response);
        }

        return true;
    }
}