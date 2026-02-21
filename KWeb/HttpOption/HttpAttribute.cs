namespace KWeb.HttpOption;

[AttributeUsage(AttributeTargets.Class)]
public class RouteAttribute : Attribute
{
    public string Path { get; }

    public RouteAttribute(string path)
    {
        Path = path;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpGetAttribute : RouteAttribute
{
    public HttpGetAttribute(string path = "") : base(path)
    {
    }
}


[AttributeUsage(AttributeTargets.Method)]
public class HttpPostAttribute : RouteAttribute
{
    public HttpPostAttribute(string path="") : base(path)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpPutAttribute : RouteAttribute
{
    public HttpPutAttribute(string path="") : base(path)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpPatchAttribute : RouteAttribute
{
    public HttpPatchAttribute(string path="") : base(path)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class HttpDeleteAttribute : RouteAttribute
{
    public HttpDeleteAttribute(string path="") : base(path)
    {
    }
}


[AttributeUsage(AttributeTargets.Method)]
public class HttpHeadAttribute : RouteAttribute
{
    public HttpHeadAttribute(string path="") : base(path)
    {
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class QueryParamAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter)]
public class RouteParamAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter)]
public class RequestBodyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter)]
public class RequestFormAttribute : Attribute
{
    public string Name { get; }

    public RequestFormAttribute(string name = "")
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class FormPartAttribute : Attribute
{
    public string Name { get; }

    public FormPartAttribute(string name = "")
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class WithCorsAttribute : Attribute
{
    public string Name { get; }

    public WithCorsAttribute(string name = "")
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class WithInterceptorAttribute : Attribute
{
    public string Name { get; }

    public WithInterceptorAttribute(string name = "")
    {
        Name = name;
    }
}


