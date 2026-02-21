namespace KWeb.ExceptionHandle;

public abstract class GlobalExceptionHandler
{
    public abstract void Handle(Exception exception);
    
    public class DefaultImpl : GlobalExceptionHandler
    {
        public override void Handle(Exception exception)
        {
            return;
        }
    }
}

public class NotExceptionTypeException : Exception
{
    public NotExceptionTypeException() : base("输入类型参数存在非异常基类的子类！")
    {
    }
}
