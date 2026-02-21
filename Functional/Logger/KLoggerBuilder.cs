using IServiceProvider = KWeb.DependencyInjection.IServiceProvider;

namespace Functional.Logger;

public class KLoggerBuilder : IKLoggerBuilder
{
    private const string LogFolder = "Logs";
    public KLoggerBuilder()
    {
        folderPath = LogFolder;
        maxDayCount = 7;
    }

    private string folderPath;
    private int maxDayCount;

    public KLoggerBuilder SetFolderPath(string folderPath)
    {
        this.folderPath = folderPath;
        return this;
    }

    public KLoggerBuilder SetMaxCount(int maxDayCount)
    {
        this.maxDayCount = maxDayCount;
        return this;
    }

    public (IKLogger, Func<IServiceProvider,IKLogger<object>>) Build()
    {
        return (new KLogger(folderPath, maxDayCount),_=>new KLogger<object>(folderPath, maxDayCount));
    }
}

public static class KLoggerBuilderExpansion
{
    public static void UseKLogger(this IKLoggerBuilder baseBuilder,Action<KLoggerBuilder> builder)
    {
        KLoggerBuilder loggerBuilder = new KLoggerBuilder();
        builder.Invoke(loggerBuilder);
    }
}