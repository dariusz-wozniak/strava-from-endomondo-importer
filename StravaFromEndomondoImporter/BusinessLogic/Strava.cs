namespace StravaFromEndomondoImporter.BusinessLogic;

public static class Strava
{
    public static async Task UploadActivity(string accessToken, Activity activity, Logger logger, Options options)
    {
        var rs = await Api.AppendPathSegments("uploads")
                          .WithOAuthBearerToken(accessToken)
                          .PostMultipartAsync(content =>
                              content.AddFile("file", activity.Path)
                                     .AddString("name", $"Workout {NameSuffix}")
                                     .AddString("data_type", "tcx")
                          )
                          .ReceiveString();

        using var store = ActivitiesDataStore.Create(options);
        var collection = store.GetCollection<Activity>();

        activity.StravaUploadId = Parse.FromJson(rs, "id").ToString();
        activity.StravaError = Parse.FromJson(rs, "error").ToString();
        activity.StravaStatus = Parse.FromJson(rs, "status").ToString();
        activity.StravaActivityId = Parse.FromJson(rs, "activity_id").ToString();
        activity.Status = Status.UploadSuccessful;

        await collection.ReplaceOneAsync(activity.Id, activity);
        logger.Information("Uploaded {ActivityFilename} successfully. Full response: {Rs}", activity.Filename, rs);
    }

    public static async Task UpdateActivity(string accessToken, Activity activity, Logger logger, Options options)
    {
        var upload = await Api.AppendPathSegments("uploads", activity.StravaUploadId)
                              .WithOAuthBearerToken(accessToken)
                              .GetStringAsync();

        var activityId = Parse.FromJson(upload, "activity_id").ToString();
        if (string.IsNullOrWhiteSpace(activityId))
        {
            logger.Warning("Failed to update {ActivityFilename} to Strava. Full response: {Upload}", activity.Filename, upload);
            return;
        }

        var stravaSportType = Map(activity.TcxActivityType);

        var update = await Api.AppendPathSegments("activities", activityId)
                              .WithOAuthBearerToken(accessToken)
                              .PutJsonAsync(new
                              {
                                  hide_from_home = true,
                                  gear_id = (string)null,
                                  sport_type = stravaSportType,
                                  name = $"{stravaSportType} {NameSuffix}",
                              })
                              .ReceiveString();

        using var store = ActivitiesDataStore.Create(options);
        var collection = store.GetCollection<Activity>();

        activity.StravaActivityId = Parse.FromJson(update, "id").ToString();
        activity.StravaError = null;
        activity.StravaStatus = null;
        activity.StravaActivityType = Parse.FromJson(update, "sport_type").ToString();
        activity.StravaName = Parse.FromJson(update, "name").ToString();
        activity.Status = Status.UploadAndUpdateSuccessful;
        activity.IsCompleted = true;

        await collection.ReplaceOneAsync(activity.Id, activity);
        logger.Information("Updated {ActivityFilename} successfully! Full response: {Rs}", activity.Filename, update);
    }

    public static async Task<(string accessToken, string refreshToken)> GetTokens(Options options, string code)
    {
        var rs = await Host.AppendPathSegments("oauth", "token")
                           .PostUrlEncodedAsync(new Dictionary<string, string>
                           {
                               { "client_id", options.ClientId },
                               { "client_secret", options.ClientSecret },
                               { "code", code },
                               { "grant_type", "authorization_code" },
                           })
                           .ReceiveString();

        var accessToken = Parse.FromJson(rs, "access_token").ToString();
        var refreshToken = Parse.FromJson(rs, "refresh_token").ToString();
        
        return (accessToken, refreshToken);
    }

    private static string Map(string tcxActivityType) =>
        tcxActivityType switch
        {
            Sports.Run.Tcx => Sports.Run.Strava,
            Sports.Biking.Tcx => Sports.Biking.Strava,
            Sports.Other.Tcx => Sports.Walk.Strava,
            _ => Sports.Walk.Strava
        };
}