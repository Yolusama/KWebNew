using System.Net;

namespace KWeb.HttpOption;

public class ActionResult
{
    public string Message { get; set; }
    public bool Success { get; set; }
    public object Data { get; set; }
}

public class ActionResult<T> : ActionResult
{
    public new T Data { get; set; }

    public static implicit operator ActionResult<T> (T data)
    {
        return new ActionResult<T> { Message = "OK",Success = true, Data = data };
    }
}