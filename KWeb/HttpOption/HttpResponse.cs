using System.Net;
using System.Text;

namespace KWeb.HttpOption;

public class HttpResponse
{
    public Dictionary<string, string> Headers { get; }
    public Dictionary<string,string> Cookies { get; }
    public Encoding Encoding { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public string StatusDescription { get; set; }
    public string Version { get; }
    public string Result { get; set; }
    
    public HttpResponse(HttpListenerResponse response)
    {
        Headers = new Dictionary<string, string>();
        Cookies = new Dictionary<string, string>();
        foreach (KeyValuePair<string,string> header in response.Headers)
            Headers.Add(header.Key, header.Value);
        foreach (KeyValuePair<string,string> cookie in response.Cookies)
            Cookies.Add(cookie.Key, cookie.Value);
        Version = response.ProtocolVersion.ToString();
        StatusDescription = response.StatusDescription;
        Encoding = response.ContentEncoding ?? Encoding.UTF8;
    }
    
    /*public string ResponseHeaders()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append($"{Version} {(int)StatusCode} {StatusCode} \r\n");
        foreach (string key in Headers.Keys)
            builder.Append($"{key}: {Headers[key]}\r\n");
        builder.Append("\r\n");
        return builder.ToString();
    }*/
    
    public void ResponseTo(HttpListenerResponse response)
    {
        using var output = response.OutputStream;
        response.Headers.Clear();
        foreach (string key in Headers.Keys)
            response.Headers.Add(key, Headers[key]);
        //string headers = ResponseHeaders();
        //var headersBytes = Encoding.GetBytes(headers);
        //output.Write(headersBytes, 0, headersBytes.Length);
        output.Write( Encoding.GetBytes(Result));
    }
}