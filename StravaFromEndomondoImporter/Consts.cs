namespace StravaFromEndomondoImporter;

public static class Consts
{
    public static string Api => $"{Host}/api/v3/";
    public static string Host => "https://www.strava.com";
    public static string RedirectUri => "http://localhost/strava_exchange_token";
    public static string ResponseType => "code";
    public static string Scope => "read_all,profile:read_all,activity:read_all,activity:write";
    public static string AuthorizeUrl => $"{Host}/oauth/authorize";
    public static string NameSuffix => "(Imported)";
}