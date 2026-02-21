using IServiceProvider = KWeb.DependencyInjection.IServiceProvider;

namespace KWeb.HttpOption;

public abstract class HttpRequestInterceptor
{
    private string[] excludedPatterns = [];
    private string[] includedPatterns = [];
    private string name;
    internal string[] ExcludedPatterns => excludedPatterns;
    internal string[] IncludedPatterns => includedPatterns;
    internal string Name => name;

    public virtual bool ToHandle(HttpRequest request,HttpResponse response)
    {
        return true;
    }

    public virtual bool Handled(HttpRequest request, HttpResponse response)
    {
        return true;
    }

    internal void From(HttpInterceptorData data)
    {
        excludedPatterns = data.ExcludedPatterns;
        includedPatterns = data.IncludedPatterns;
        name = data.Name;
    }

    public class DefaultImpl : HttpRequestInterceptor
    {
        
    }
}

internal struct HttpInterceptorData
{
    public string Name { get; set; }
    public string[] ExcludedPatterns { get; set; }
    public string[] IncludedPatterns { get; set; }
        
    public HttpInterceptorData()
    {
        ExcludedPatterns = [];
        IncludedPatterns = [];
    }
}

public class HttpRequestInterceptorBuilder<T> where T: HttpRequestInterceptor
{
    private HttpInterceptorData interceptorData = new();
    internal HttpRequestInterceptor Instance { get; private set; }
    
    public HttpRequestInterceptorBuilder<T> WithName(string name)
    {
        interceptorData.Name = name;
        return this;
    }
    
    public HttpRequestInterceptorBuilder<T> ExcludedPatterns(params string[] excludedPatterns)
    {
        interceptorData.ExcludedPatterns = excludedPatterns;
        return this;
    }

    public HttpRequestInterceptorBuilder<T> IncludedPatterns(params string[] includedPatterns)
    {
        interceptorData.IncludedPatterns = includedPatterns;
        return this;
    }
    
    public HttpRequestInterceptor Build(Func<HttpRequestInterceptor> instanceFunc)
    {
        Instance = instanceFunc.Invoke();
        Instance.From(interceptorData);
        return Instance;
    }
    
    public HttpRequestInterceptor Build()
    {
        Instance = Activator.CreateInstance<T>();
        Instance.From(interceptorData);
        return Instance;
    }
}
