using Functional.Logger;
using KWeb.BaseServices;
using KWeb.Common;
using KWeb.DependencyInjection;
using KWeb.ExceptionHandle;
using KWeb.HttpOption;
using IServiceProvider = KWeb.DependencyInjection.IServiceProvider;


namespace KWeb.Expansion;

public static class AppExpansions
{
    public static void AddConfiguration<T>(this IServiceProvider services,
        Func<IServiceProvider, T> config, string name = "")
    {
            services.AddSingle(config, name);
    }
    
    public static void AddLogger(this IServiceProvider services,Action<IKLoggerBuilder> builder)
    {
           builder.Invoke(IKLoggerBuilder.Default);
    }

    public static void UseDefaultLogger(this WebApplication app)
    {
        var services = app.Services;
        var configuration = services.Get<IConfiguration>();
        var loggerConfig = configuration["Logger"];
        var folderPath = loggerConfig?.Get<string>("FolderPath");
        int maxDayCount = loggerConfig?.Get<int>("MaxDayCount") ?? 7;
        if (!app.Services.HasInjected<IKLogger>(nameof(IKLogger)))
        {
            app.Services.AddSingle<IKLogger,KLogger>(_=>new KLogger(folderPath, maxDayCount));
            app.Services.AddService<IKLogger<object>,KLogger<object>>(_=>new KLogger<object>(folderPath, maxDayCount));
        }
    }

    public static void AddCors(this IServiceProvider services,Action<HttpCorsVerifierBuilder> builder)
    {
        var corsBuilder = new HttpCorsVerifierBuilder();
        builder.Invoke(corsBuilder);
        string instanceName = string.IsNullOrEmpty(corsBuilder.Name) ? Constants.DefaultCors : corsBuilder.Name;
        services.AddSingle<HttpCorsVerifier>(_=>corsBuilder.Inner,instanceName);
    }

    public static void UseCors(this WebApplication app)
    {
        app.Services.AddSingle<bool>(Constants.CorsServiceName);
    }

    public static void AddGlobalExceptionHandler<T>(this IServiceProvider services) where T: GlobalExceptionHandler
    {
        services.AddService<GlobalExceptionHandler,T>();
    }

    public static void AddInterceptor<T>(this IServiceProvider services,
        Action<HttpRequestInterceptorBuilder<T>> builderFunc) where T:HttpRequestInterceptor
    {
        HttpRequestInterceptorBuilder<T> builder = new HttpRequestInterceptorBuilder<T>(); 
        builderFunc.Invoke(builder);
        string serviceName = string.IsNullOrEmpty(builder.Instance.Name) ? Constants.DefaultInterceptor 
            : builder.Instance.Name;
        services.AddService<HttpRequestInterceptor,T>(_=>builder.Instance as T,serviceName);
    }

    public static void UseInterceptor(this WebApplication app)
    {
       app.Services.AddSingle<bool>(Constants.InterceptorServiceName);
    }

    public static void UseGlobalExceptionHandler(this WebApplication app)
    {
        app.Services.AddSingle<bool>(Constants.GlobalExceptionHandler);
    }
    
}