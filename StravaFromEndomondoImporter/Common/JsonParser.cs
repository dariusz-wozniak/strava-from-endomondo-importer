namespace StravaFromEndomondoImporter.Common;

public interface IJsonParser
{
    object FromJson(string json, string key);
}

public class JsonParser : IJsonParser
{
    public object FromJson(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        if (key == null) throw new ArgumentNullException(nameof(key));
        
        return JObject.Parse(json)[key];
    }
}