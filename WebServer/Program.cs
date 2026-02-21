// See https://aka.ms/new-console-template for more information

using Functional.Logger;
using KWeb;
using KWeb.BaseServices;
using KWeb.ExceptionHandle;
using KWeb.Expansion;using KWeb.HttpOption;

var builder = WebApplicationBuilder.Create(args);
builder.Services.AddLogger(builder1 =>
{
    var services = builder.Services;
    builder1.UseKLogger(builder2 =>
    {
        var configuration = services.Get<IConfiguration>();
        var loggerConfig = configuration["Logger"];
        var folderPath = loggerConfig?.Get<string>("FolderPath");
        int maxDayCount = loggerConfig?.Get<int>("MaxDayCount") ?? 7;
        var loggerTuple = builder2.SetFolderPath(folderPath)
            .SetMaxCount(maxDayCount)
            .Build();
        services.AddSingle<IKLogger,KLogger>(_=> loggerTuple.Item1 as KLogger);
        services.AddService<IKLogger<object>,KLogger<object>>(_=>loggerTuple.Item2.Invoke(_) as KLogger<object>);
    });
});
builder.Services.AddService<string>(_=>Random.Shared.Next(int.MinValue,int.MaxValue).ToString());
builder.Services.AddCors(builder =>
{
    builder.AllowAnyMethods()
        .AllowAnyHeaders()
        .AllowAnyOrigins()
        .Build();
});
builder.Services.AddInterceptor<HttpRequestInterceptor.DefaultImpl>(builder1 =>
{
    builder1.WithName("1")
        .Build();
});
builder.Services.AddGlobalExceptionHandler<GlobalExceptionHandler.DefaultImpl>();

var app = builder.Build();

app.AutoConfig();
app.HandleInjectionServices();
app.RegisterAllWebPaths();
app.UseCors();
app.UseInterceptor();
app.UseGlobalExceptionHandler();


app.Run();

//Console.WriteLine("Hello, World!");