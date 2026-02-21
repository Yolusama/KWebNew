using System.Text.Json;

namespace KWeb.JSON;

public class JsonReader : IDisposable
{
    private readonly JsonDocument document;

    public JsonReader(Stream stream)
    {
        document = JsonDocument.Parse(stream);
    }

    public void Dispose()
    {
        document.Dispose();
    }

    public JsonInfo? this[string key] => Read(key);

    public JsonInfo? Read(string key)
    {
        try
        {
            JsonElement? res = null;
            if (key.Contains(':'))
            {
                string[] keys = key.Split(':');
                JsonElement element = document.RootElement.GetProperty(keys[0]);
                for (int i = 1; i < keys.Length; i++)
                {
                    element = element.GetProperty(keys[i]);
                }

                res = element;
            }
            else
                res = document.RootElement.GetProperty(key);

            return new JsonInfo(res.Value);
        }
        catch
        {
            return default;
        }
    }
}