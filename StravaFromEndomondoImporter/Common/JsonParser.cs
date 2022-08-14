namespace StravaFromEndomondoImporter.Common;

public interface IJsonParser
{
    object FromJson(string json, string key);
}

public class JsonParser : IJsonParser
{
    // TODO: replace by System.Text.Json https://stackoverflow.com/q/58271901/297823
    public object FromJson(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        if (key == null) throw new ArgumentNullException(nameof(key));
        
        return JObject.Parse(json)[key];
    }
}