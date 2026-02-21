using System.Collections.Frozen;

namespace KWeb.HttpOption;




public class HttpCorsVerifier
{
    private List<string> allowedOrigins = [];
    private List<string> allowedHeaders = [];
    private List<string> allowedMethods = [];
    private int maxAge = 3600;
    private bool allowCredentials = false;
    internal string Name { get; set; }

    private FrozenSet<string> AllowedOrigins => allowedOrigins.ToFrozenSet();
    private FrozenSet<string> AllowedHeaders => allowedHeaders.ToFrozenSet();
    private FrozenSet<string> AllowedMethods => allowedMethods.ToFrozenSet();

    private const string AccessHeader = "Access-Control-Allow-Headers";
    private const string AccessOriginHeader = "Access-Control-Allow-Origin";
    private const string AccessMethodHeader = "Access-Control-Allow-Methods";
    private const string AccessMaxAgeHeader = "Access-Control-Max-Age";
    private const string AccessCredentialsHeader = "Access-Control-Allow-Credentials";

    public void AddCorsHeaders(HttpResponse response)
    {
        response.Headers.Add(AccessMaxAgeHeader, maxAge.ToString());
        if (AllowedHeaders.Count > 0)
            response.Headers.Add(AccessHeader, string.Join("\n", AllowedHeaders.ToList()));
        if (AllowedOrigins.Count > 0)
            response.Headers.Add(AccessOriginHeader, string.Join("\n", AllowedOrigins.ToList()));
        if (AllowedMethods.Count > 0)
            response.Headers.Add(AccessMethodHeader, string.Join("\n", AllowedMethods.ToList()));
        if (allowCredentials)
            response.Headers.Add(AccessCredentialsHeader, "true");
    }

    public bool Verify(HttpRequest request)
    {
        if (AllowedOrigins.Count > 0)
        {
            if(!AllowedOrigins.Contains(request.UserHostName))
                return false;
        }
        if (AllowedMethods.Count > 0)
        {
            if (!AllowedMethods.Contains(request.Method))
                return false;
        }

        if (AllowedHeaders.Count > 0)
        {
            foreach (var header in request.Headers)
            {
                if(!AllowedHeaders.Contains(header.Key))
                    return false;
            }
        }
        
        
        return true;
    }

    internal void AddOrigins(params string[] origins)
    {
        allowedOrigins.AddRange(origins);
    }

    internal void AddHeaders(params string[] headers)
    {
        allowedHeaders.AddRange(headers);
    }

    internal void AddMethods(params string[] methods)
    {
        allowedOrigins.AddRange(methods);
    }

    internal void SetMaxAge(int maxAge)
    {
        this.maxAge = maxAge;
    }

    internal void AllowCredentials()
    {
        allowCredentials = true;
    }
}

public class HttpCorsVerifierBuilder
{
    internal string Name => corsData.Name;
    private struct CorsData
    {
        public string[] AllowedOrigins { get; set; }
        public string[] AllowedHeaders { get; set; }
        public string[] AllowedMethods { get; set; }
        public int MaxAge { get; set; }
        public bool AllowCredentials { get; set; }
        public string Name { get; set; }

        public CorsData()
        {
            MaxAge = 3600;
            AllowCredentials = false;
            AllowedOrigins = [];
            AllowedHeaders = [];
            AllowedMethods = [];
        }
    }
    private CorsData corsData = new CorsData();
    internal HttpCorsVerifier Inner { get;private set; }
    private readonly string[] allMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD" };

    public HttpCorsVerifierBuilder CorsVerifierName(string name)
    {
        corsData.Name = name;
        return this;
    }
    public HttpCorsVerifierBuilder AllowOrigins(params string[] origins)
    {
        corsData.AllowedOrigins = origins;
        return this;
    }

    public HttpCorsVerifierBuilder AllowAnyOrigins()
    {
       return AllowOrigins("*");
    }

    public HttpCorsVerifierBuilder AllowHeaders(params string[] headers)
    {
        corsData.AllowedHeaders = headers;
        return this;
    }
    public HttpCorsVerifierBuilder AllowAnyHeaders()
    {
      return AllowHeaders("*");
    } 
    public HttpCorsVerifierBuilder AllowMethods(params string[] methods)
    {
        corsData.AllowedMethods = methods;
        return this;
    }

    public HttpCorsVerifierBuilder AllowAnyMethods()
    {
        return AllowMethods(allMethods);
    }

    public HttpCorsVerifierBuilder AllowCredentials()
    {
        corsData.AllowCredentials = true;
        return this;
    }

    public HttpCorsVerifierBuilder SetMaxAge(int maxAge)
    {
        corsData.MaxAge = maxAge;
        return this;
    }

    public HttpCorsVerifier Build()
    {
        var verifier = new HttpCorsVerifier();
        if(corsData.AllowedOrigins.Length > 0)
           verifier.AddOrigins(corsData.AllowedOrigins);
        if(corsData.AllowedHeaders.Length > 0)
           verifier.AllowCredentials();
        if(corsData.AllowedMethods.Length > 0)
            verifier.AddMethods(corsData.AllowedMethods);
        if(corsData.AllowCredentials)
           verifier.AllowCredentials();
        verifier.SetMaxAge(corsData.MaxAge);
        Inner = verifier;
        return Inner;
    }
}