namespace StravaFromEndomondoImporter.Common;

public static class Parse
{
    public static object FromJson(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        if (key == null) throw new ArgumentNullException(nameof(key));
        
        return JObject.Parse(json)[key];
    }
}