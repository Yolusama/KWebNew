namespace KWeb.HttpOption.HttpOptionBuliderException;

public class BufferSizeOverLargeException : Exception
{
    public BufferSizeOverLargeException() : base("处理请求的缓冲区大小不超过最大大小的十分之一!")
    {
    }
}

public class RequestSizeOverflowException : Exception
{
    public RequestSizeOverflowException() : base("请求体大小超过最大承载量！")
    {
    }
}

public class RouteRepeateException : Exception
{
    public RouteRepeateException() : base("重复注册HTTP访问路径！") { }
}

public class MultiRequestBodyException : Exception
{
    public MultiRequestBodyException() : base("请求体只能定义一个！") { }
}