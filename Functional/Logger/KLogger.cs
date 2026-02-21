using System.Text;

namespace Functional.Logger;

public class KLogger : IKLogger
{
    public KLogger(string? folderPath = null,int maxDayCount = 7)
        { 
            FolderPath = folderPath;
            MaxDayCount = maxDayCount;
            if(folderPath != null)
            {
                DirectoryInfo directory = new DirectoryInfo(folderPath);
                if(!directory.Exists)
                    directory.Create();
            }
        }
        public string? FolderPath { get; }
        public int MaxDayCount { get; }

        public virtual void Log(string message, LogLevel level)
        {
            SwitchLogColor(level);
            string toWriteMsg = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}，Log Level {level}: {message}";
            if(level == LogLevel.Error || level == LogLevel.Fatal)
                Console.Error.WriteLine(toWriteMsg);
            else
                Console.WriteLine(toWriteMsg);
            if(FolderPath != null)
            {
                DirectoryInfo directory = new DirectoryInfo(FolderPath);
                var files = directory.GetFiles();
                if (files.Length == MaxDayCount)
                    files.FirstOrDefault()?.Delete();
                string path = Path.Combine(FolderPath, $"{DateTime.Now.ToString("yyyy-MM-dd")}.log");
                using FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Write);
                stream.Write(Encoding.UTF8.GetBytes(toWriteMsg+'\n'));
            }
            Console.ResetColor();
        }
        public void Debug(string message)
        {
            Log(message,LogLevel.Debug);
        }

        public void Error(string message)
        {
            Log(message,LogLevel.Error);
        }

        public void Fatal(string message)
        {
            Log(message,LogLevel.Fatal);
        }

        public void Info(string message)
        {
            Log(message,LogLevel.Info);
        }


        public void Trace(string message)
        {
            Log(message, LogLevel.Trace);
        }

        public void AppStart(string message)
        {
            throw new NotImplementedException();
        }

        public void AppEnd(string message)
        {
            throw new NotImplementedException();
        }

        public void Warn(string message)
        {
            Log(message, LogLevel.Warn);
        }

        private void SwitchLogColor(LogLevel level)
        {
            switch(level)
            {
                case LogLevel.Trace: Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                case LogLevel.Debug: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case LogLevel.Info: Console.ForegroundColor = ConsoleColor.Magenta; break;
                case LogLevel.Warn: Console.ForegroundColor = ConsoleColor.DarkYellow;break;
                case LogLevel.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                case LogLevel.Fatal: Console.ForegroundColor = ConsoleColor.DarkRed; break;
            }
        }
}

public class KLogger<Inner> : IKLogger<Inner> where Inner : class
{
    private readonly InnerLogger logger;
    public KLogger(string folderPath = null, int maxDayCount = 7)
    {
        logger = new InnerLogger(folderPath, maxDayCount);
    }

    private class InnerLogger : KLogger
    {
        private  readonly Type innerType  = typeof(Inner);
        public InnerLogger(string? folderPath = null, int maxDayCount = 7) : base(folderPath, maxDayCount)
        {
        }

        public override void Log(string message, LogLevel level)
        {
            string srcMsg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},日志输出自{innerType.FullName}，类{innerType.Name}中:";
            var builder = new StringBuilder();
            builder.AppendLine(srcMsg);
            builder.AppendLine(message);
            base.Log(builder.ToString(), level);
        }
    }

    public void Log(string message, LogLevel level)
    {
        logger.Log(message, level);
    }

    public void Debug(string message)
    {
        logger.Debug(message);
    }

    public void Info(string message)
    {
        logger.Info(message);
    }

    public void Warn(string message)
    {
        logger.Warn(message);
    }

    public void Error(string message)
    {
        logger.Error(message);
    }

    public void Fatal(string message)
    {
        logger.Fatal(message);
    }

    public void Trace(string message)
    {
        logger.Trace(message);
    }
}