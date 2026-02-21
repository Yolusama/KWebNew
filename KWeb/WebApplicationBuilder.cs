using System.Reflection;
using KWeb.BaseServices;
using KWeb.BaseServices.Impl;
using KWeb.DependencyInjection;
using KWeb.JSON;
using IServiceProvider = KWeb.DependencyInjection.IServiceProvider;
namespace KWeb;

public class WebApplicationBuilder
{
   private const string JsonFileName = "application.json";
   private readonly WebApplicationBuilder instance = null;
   public IServiceProvider Services { get;}
   public IConfiguration Configuration { get; }
   public string[] Args { get; }

   private WebApplicationBuilder(string []args)
   {
      if (instance != null)
         throw new SystemException("只能创建一个WebApplicationBuilder实例");
      Services = new ServiceProvider();
      KJSON json = new KJSON(JsonFileName);
      var configuration = new KConfiguration(json);
      Services.AddSingle<IConfiguration,KConfiguration>(_=> configuration);
      Configuration = configuration;
      Args = args;
      instance = this;
   }
   
   public WebApplication Build()
   {
      return new WebApplication(Services, Args);
   }
   
   public static WebApplicationBuilder Create(string[] args)
   {
      return new WebApplicationBuilder(args);
   }
   
}