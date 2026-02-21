using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Functional.Util;

namespace KWeb.HttpOption;

public class HttpRequest
{
    public Dictionary<string, string> Headers { get; set; }
    public Dictionary<string, string> Query { get; set; }
    public Dictionary<string, string> Cookies { get; set; }
    public string Method { get; set; }
    public string Path { get; }
    public string Body { get; }
    public string UserAgent { get; }
    public object Form { get; }
    public string FullPath { get; }
    public string UserHost { get; }
    public string UserHostName { get; }
    public IPEndPoint RemoteIP { get; }
    public IPEndPoint LocalIP { get; }
    public Encoding Encoding { get; set; }

    public HttpRequest(HttpListenerRequest request)
    {
        Headers = new Dictionary<string, string>();
        Query = new Dictionary<string, string>();
        Cookies = new Dictionary<string, string>();
        foreach (var key in request.Headers.AllKeys)
            Headers.Add(key, request.Headers[key]);
        foreach (var key  in request.QueryString.AllKeys)
            Query.Add(key, request.QueryString[key]);
        foreach (KeyValuePair<string, string> cookie in request.Cookies)
            Cookies.Add(cookie.Key, cookie.Value);
        RemoteIP = request.RemoteEndPoint;
        LocalIP = request.LocalEndPoint;
        Method = request.HttpMethod;
        UserAgent = request.UserAgent;
        Encoding = request.ContentEncoding;
        if (request.HasEntityBody)
        {
            using MemoryStream ms = new();
            request.InputStream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            Body = Encoding.GetString(ms.ToArray());
        }

        Path = request.RawUrl;
        FullPath = request.Url.AbsoluteUri;
        UserHost = request.UserHostAddress;
        UserHostName = request.UserHostName;
    }
}