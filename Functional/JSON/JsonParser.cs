using System.Reflection;
using System.Text.Json;

namespace KWeb.JSON;

public class JsonParser : IDisposable
{
    private readonly JsonDocument document;

    public JsonParser(string jsonStr)
    {
        document = JsonDocument.Parse(jsonStr);
    }

    public void Dispose()
    {
        document.Dispose();
    }

    public object? Parse(string key, Type type)
    {
        return document.RootElement.GetProperty(key).Deserialize(type);
    }

    public object? Parse(Type type)
    {
        var pros = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        object? obj = Activator.CreateInstance(type);
        foreach (var pro in pros)
        {
            Type proType = pro.PropertyType;
            JsonElement element;
            if (!document.RootElement.TryGetProperty(pro.Name, out element)) continue;
            pro.SetValue(obj, element.Deserialize(proType));
        }

        return obj;
    }

    public T? Parse<T>()
    {
        Type type = typeof(T);
        var pros = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        T? obj = (T?)Activator.CreateInstance(type);
        foreach (var pro in pros)
        {
            Type proType = pro.PropertyType;
            JsonElement element;
            if (!document.RootElement.TryGetProperty(pro.Name, out element)) continue;
            pro.SetValue(obj, element.Deserialize(proType));
        }

        return obj;
    }

    public T? Parse<T>(string key)
    {
        return document.RootElement.GetProperty(key).Deserialize<T>();
    }
}