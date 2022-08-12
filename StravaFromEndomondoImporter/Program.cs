var options = Parser.Default.ParseArguments<Options>(args).Value;
var logger = Logging.Setup(options.Path);

try
{
    if (options.SkipScanning) logger.Information("Skipping scanning");
    else await FileScanner.Scan(options, logger);

    var (processed, total) = ActivitiesDataStore.GetStats(options);
    logger.Information("Processed {Processed} of {Total} activities ({Percentage}%)", processed, total, (processed * 100) / total);

    var url = $"{AuthorizeUrl}?client_id={options.ClientId}&response_type={ResponseType}&redirect_uri={RedirectUri}&scope={Scope}";
    url = url.Replace("&", "^&");

    if (!BrowserRunner.RunBrowser(url)) return;
    Console.WriteLine("code=");
    var code = Console.ReadLine()?.Trim() ?? string.Empty;

    var (accessToken, refreshToken) = await Strava.GetTokens(options, code);

    // TODO: add start of loop x batch here

    // Uploading:
    // var toBeUploaded = ActivitiesDataStore.GetActivities(options, Status.AddedToDataStoreWithDetails, take: Configuration.BatchSize);
    // logger.Information("Uploading {ActivitiesCount} activities", toBeUploaded.Count);
    // foreach (var activity in toBeUploaded) await Strava.UploadActivity(accessToken, activity, logger, options);

    // Updating:
    var toBeUpdated = ActivitiesDataStore.GetActivities(options, Status.UploadSuccessful, take: Configuration.BatchSize);
    logger.Information("Updating {ActivitiesCount} activities", toBeUpdated.Count);
    foreach (var activity in toBeUpdated) await Strava.UpdateActivity(accessToken, activity, logger, options);
}
catch (Exception e)
{
    logger.Error(e, e?.Message);
    Console.ReadKey();
    Environment.Exit(0);
}


// for later:
/*

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