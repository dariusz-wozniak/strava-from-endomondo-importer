var options = Parser.Default.ParseArguments<Options>(args).Value;
var logger = Logging.Setup(options.Path);

try
{
    if (options.SkipScanning) logger.Information("Skipping scanning");
    else await FileScanner.Scan(options, logger);

    var url =
        $"{AuthorizeUrl}?client_id={options.ClientId}&response_type={ResponseType}&redirect_uri={RedirectUri}&scope={Scope}";
    url = url.Replace("&", "^&");

    if (!BrowserRunner.RunBrowser(url)) return;
    Console.WriteLine("code=");
    var code = Console.ReadLine()?.Trim() ?? string.Empty;

    var tokenRs = await "https://www.strava.com".AppendPathSegments("oauth", "token")
                                                .PostUrlEncodedAsync(new Dictionary<string, string>
                                                {
                                                    { "client_id", options.ClientId },
                                                    { "client_secret", options.ClientSecret },
                                                    { "code", code },
                                                    { "grant_type", "authorization_code" },
                                                })
                                                .ReceiveString();

    var accessToken = Parse.FromJson(tokenRs, "access_token").ToString();
    var refreshToken = Parse.FromJson(tokenRs, "refresh_token").ToString();

    using var store = ActivitiesDataStore.Create(options);
    var collection = store.GetCollection<Activity>();
    var activities = collection.AsQueryable()
                               .Where(x => !x.IsCompleted && x.Status == Status.AddedToDataStoreWithDetails)
                               .ToList();

    // Uploading:
    logger.Information("Uploading {ActivitiesCount} activities", activities.Count);
    foreach (var activity in activities)
    {
        var rs = await Api.AppendPathSegments("uploads")
                           .WithOAuthBearerToken(accessToken)
                           .PostMultipartAsync(content =>
                               content.AddFile("file", activity.Path)
                                      .AddString("name", $"Workout {NameSuffix}")
                                      .AddString("data_type", "tcx")
                           )
                           .ReceiveString();

        activity.StravaUploadId = Parse.FromJson(rs, "id").ToString();
        activity.StravaError = Parse.FromJson(rs, "error").ToString();
        activity.StravaStatus = Parse.FromJson(rs, "status").ToString();
        activity.StravaActivityId = Parse.FromJson(rs, "activity_id").ToString();
        activity.Status = Status.UploadSuccessful;

        await collection.ReplaceOneAsync(activity.Id, activity);
        logger.Information("Uploaded {ActivityFilename} successfully. Full response: {Rs}", activity.Filename, rs);
    }
}
catch (Exception e)
{
    logger.Error(e, e?.Message);
    Console.ReadKey();
    Environment.Exit(0);
}

// for later:
/*

var rs = await apiHost.AppendPathSegments("uploads")
                   .WithOAuthBearerToken(accessToken)
                   .AllowAnyHttpStatus()
                   .PostMultipartAsync(content => 
                       content.AddFile("file", tcx)
                              .AddString("name", "👀 Uploaded from api")
                              .AddString("data_type", "tcx")
                   )
                   .ReceiveString();

var id = JObject.Parse(rs)["id"].ToString();

// TODO:
// Get uploads
// Update (e.g. gear)

var rsPut = await apiHost.AppendPathSegments("activities", id)
                      .WithOAuthBearerToken(accessToken)
                      .AllowAnyHttpStatus()
                      .PutJsonAsync(new
                      {
                          hide_from_home = true
                      })
                      .ReceiveString();


Console.ReadKey();

*/