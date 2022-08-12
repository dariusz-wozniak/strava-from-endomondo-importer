namespace StravaFromEndomondoImporter;

public static class Consts
{
    public static string ApiHost => "https://www.strava.com/api/v3/";
    public static string RedirectUri => "http://localhost/strava_exchange_token";
    public static string ResponseType => "code";
    public static string Scope => "read_all,profile:read_all,activity:read_all,activity:write";
    public static string AuthorizeUrl => "https://www.strava.com/oauth/authorize";
}