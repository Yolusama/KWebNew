using KWeb.JSON;

namespace KWeb.BaseServices.Impl;

public class KConfiguration(KJSON json) : IConfiguration
{
    public JsonInfo this[string key] => json[key];

    public T Get<T>(string key)
    {
        return json.Get<T>(key);
    }

    public object Get(Type type, string key)
    {
        return json.Get(type, key);
    }
}