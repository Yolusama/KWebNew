using System.Reflection;
using KWeb.DependencyInjection;
using KWeb.JSON;

namespace KWeb.BaseServices;

public interface IConfiguration
{
    public JsonInfo this[string key] { get;}
    public T Get<T>(string key);
    public object Get(Type type,string key);
}

public static class ConfigurationExtensions
{
    public static void AddAutoConfiguration(this WebApplication app,IEnumerable<Type> types)
    {
        var configTypes = types.Where(t => t.GetCustomAttribute<ConfigAttribute>()!=null);
        var configuration = app.Services.Get<IConfiguration>();
        foreach (var type in configTypes)
        {
            var attr = type.GetCustomAttribute<ConfigAttribute>();
            if(string.IsNullOrEmpty(attr.Key))
                continue;
            app.Services.AddSingle(type,_=>configuration[attr.Key].Get(type));
        }
    }
}