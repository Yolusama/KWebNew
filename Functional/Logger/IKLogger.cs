namespace Functional.Logger;
using IServiceProvider = KWeb.DependencyInjection.IServiceProvider;

public interface IKLogger
{
    public void Log(string message,LogLevel level);
    public void Debug(string message);
    public void Info(string message);
    public void Warn(string message);
    public void Error(string message);
    public void Fatal(string message);
    public void Trace(string message);
}
public interface IKLogger<in Inner> : IKLogger where Inner : class
{
}

public interface IKLoggerBuilder
{
    public static IKLoggerBuilder Default { get; } = new DefaultImp();
    public (IKLogger,Func<IServiceProvider,IKLogger<object>>) Build();

    private class DefaultImp : IKLoggerBuilder
    {
        public (IKLogger, Func<IServiceProvider, IKLogger<object>>) Build()
        {
           return (null, null);
        }
    }
}
