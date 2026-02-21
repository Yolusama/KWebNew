using System.Net;

namespace KWeb.HttpOption;

using FileResult = ActionResult<byte[]>;

public abstract class ControllerBase
{
    protected readonly HttpRequest request;
    protected readonly HttpResponse response;
    public HttpRequest Request => request;
    public HttpResponse Response => response;

    public ActionResult<T> OK<T>(T data,string msg = "OK")
    {
        response.StatusCode = HttpStatusCode.OK;

        ActionResult<T> result = new ActionResult<T>
        {
            Message = msg,
            Success = true,
            Data = data
        };
        return result;
    }

    public ActionResult OK(string msg = "OK")
    {
        response.StatusCode = HttpStatusCode.OK;

        ActionResult result = new ActionResult
        {
            Message = msg,
            Success = true,
        };

        return result;
    }

    public ActionResult NotFound(string msg = "Not Found")
    {
        response.StatusCode = HttpStatusCode.NotFound;
        
        return new ActionResult
        {
            Message = msg,
            Success = false,
        };
    }

    public ActionResult BadRequest(string msg = "BadRequest")
    {
        response.StatusCode = HttpStatusCode.BadRequest;
        
        return new ActionResult
        {
            Message = msg,
            Success = false,
        };
    }

    public ActionResult InternalServerError(string msg = "InternalServerError")
    {
        response.StatusCode = HttpStatusCode.InternalServerError;

        return new ActionResult
        {
             Message = msg,
             Success = false
        };
    }

    public FileResult File(string fileName, string contentType = "octet-stream")
    {
        using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        response.Headers.Add("Content-Type", contentType);
        return OK(ms.ToArray());
    }
    
    public FileResult File(byte[] data, string contentType = "octet-stream")
    {
        response.Headers.Add("Content-Type", contentType);
        return OK(data);
    }
}
