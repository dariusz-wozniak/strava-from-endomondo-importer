namespace StravaFromEndomondoImporter.BusinessLogic;

public interface IStravaService
{
    Task UploadActivity(string accessToken, Activity activity, Logger logger, Options options);
    Task UpdateActivity(string accessToken, Activity activity, Logger logger, Options options);
    Task<(string accessToken, string refreshToken)> GetTokens(Options options, string code);
}

public class StravaServiceService : IStravaService
{
    private readonly IJsonParser _jsonParser;
    private readonly IActivitiesDataStore _activitiesDataStore;

    public StravaServiceService(IJsonParser jsonParser,
                                IActivitiesDataStore activitiesDataStore)
    {
        _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
        _activitiesDataStore = activitiesDataStore ?? throw new ArgumentNullException(nameof(activitiesDataStore));
    }

    public async Task UploadActivity(string accessToken, Activity activity, Logger logger, Options options)
    {
        var rs = await Api.AppendPathSegment("uploads")
                          .WithOAuthBearerToken(accessToken)
                          .PostMultipartAsync(content =>
                              content.AddFile("file", activity.Path)
                                     .AddString("name", $"Workout {NameSuffix}")
                                     .AddString("data_type", "tcx")
                          )
                          .ReceiveString();

        using var store = _activitiesDataStore.Create(options);
        var collection = store.GetCollection<Activity>();

        activity.StravaUploadId = _jsonParser.FromJson(rs, "id").ToString();
        activity.StravaError = _jsonParser.FromJson(rs, "error").ToString();
        activity.StravaStatus = _jsonParser.FromJson(rs, "status").ToString();
        activity.StravaActivityId = _jsonParser.FromJson(rs, "activity_id").ToString();
        activity.Status = Status.UploadSuccessful;

        await collection.ReplaceOneAsync(activity.Id, activity, upsert: true);
        logger.Information("Uploaded {ActivityFilename} successfully. Full response: {Rs}", activity.Filename, rs);
    }

    public async Task UpdateActivity(string accessToken, Activity activity, Logger logger, Options options)
    {
        var upload = await Api.AppendPathSegments("uploads", activity.StravaUploadId)
                              .WithOAuthBearerToken(accessToken)
                              .GetStringAsync();

        var activityId = _jsonParser.FromJson(upload, "activity_id").ToString();
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

        using var store = _activitiesDataStore.Create(options);
        var collection = store.GetCollection<Activity>();

        activity.StravaActivityId = _jsonParser.FromJson(update, "id").ToString();
        activity.StravaError = null;
        activity.StravaStatus = null;
        activity.StravaActivityType = _jsonParser.FromJson(update, "sport_type").ToString();
        activity.StravaName = _jsonParser.FromJson(update, "name").ToString();
        activity.Status = Status.UploadAndUpdateSuccessful;
        activity.IsCompleted = true;

        await collection.ReplaceOneAsync(activity.Id, activity, upsert: true);
        logger.Information("Updated {ActivityFilename} successfully! Full response: {Rs}", activity.Filename, update);
    }

    public async Task<(string accessToken, string refreshToken)> GetTokens(Options options, string code)
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

        var accessToken = _jsonParser.FromJson(rs, "access_token").ToString();
        var refreshToken = _jsonParser.FromJson(rs, "refresh_token").ToString();
        
        return (accessToken, refreshToken);
    }

    private string Map(string tcxActivityType) =>
        tcxActivityType switch
        {
            Sports.Run.Tcx => Sports.Run.Strava,
            Sports.Biking.Tcx => Sports.Biking.Strava,
            Sports.Other.Tcx => Sports.Walk.Strava,
            _ => Sports.Walk.Strava
        };
}