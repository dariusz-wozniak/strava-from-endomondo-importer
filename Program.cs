var services = new ServiceCollection();

services.Scan(x => x.FromAssembliesOf(typeof(Program))
                    .AddClasses(filter => filter.Where(type => !typeof(Attribute).IsAssignableFrom(type)))
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsImplementedInterfaces());

await using var sp = services.BuildServiceProvider();

var activityFileScanner = sp.GetRequiredService<IActivityFileScanner>();
var endomondoJsonSync = sp.GetRequiredService<IEndomondoJsonSync>();
var stravaService = sp.GetRequiredService<IStravaService>();
var activitiesDataStore = sp.GetRequiredService<IActivitiesDataStore>();

var options = Parser.Default.ParseArguments<Options>(args).Value;

var logger = Logging.Setup(options.Path);

try
{
    // Step 1: Scan .TCX files and build/update database
    if (options.SkipScanning) logger.Information("Skipping scanning of TCX files");
    else await activityFileScanner.Scan(options, logger);

    ShowStats(options, logger);

    var url = $"{AuthorizeUrl}?client_id={options.ClientId}&response_type={ResponseType}&redirect_uri={RedirectUri}&scope={Scope}";
    url = url.Replace("&", "^&");

    // Step 2: Get the authorization code
    if (!BrowserRunner.RunBrowser(url)) return;
    Console.WriteLine("code=");
    var code = Console.ReadLine()?.Trim() ?? string.Empty;

    // Step 3: Authorize and fetch Bearer token
    var (accessToken, refreshToken) = await stravaService.GetTokens(options, code);
    var policy = Policies.RetryPolicy(logger);

    while (true)
    {
        await policy.ExecuteAndCaptureAsync(async () =>
        {
            ShowStats(options, logger);

            // Step 4: Upload to Strava
            var toBeUploaded = activitiesDataStore.GetActivities(options, Status.AddedToDataStoreWithDetails, take: BatchSizeForUploading);
            logger.Information("Uploading {ActivitiesCount} activities", toBeUploaded.Count);
            foreach (var activity in toBeUploaded) await stravaService.UploadActivity(accessToken, activity, logger, options);

            // Step 5: Update Strava activities (set gear ID to null, etc.)
            var toBeUpdated = activitiesDataStore.GetActivities(options, Status.UploadSuccessful, take: BatchSizeForUpdating);
            logger.Information("Updating {ActivitiesCount} activities", toBeUpdated.Count);
            foreach (var activity in toBeUpdated) await stravaService.UpdateActivity(accessToken, activity, logger, options);

            // Step 6: Scan Endomondo .JSON files & make updates in Strava if activity types are different
            bool needsToSyncActivities = false;
            if (options.SyncWithEndomondoJsons)
            {
                await endomondoJsonSync.Scan(options, logger);
                needsToSyncActivities = await endomondoJsonSync.Update(options, logger, accessToken);
            }
            
            if (!toBeUpdated.Any() && !toBeUploaded.Any() && !needsToSyncActivities)
            {
                logger.Information("PROCESSED ALL! 🎉 - No activities to upload or update");
                ShowStats(options, logger);
                Environment.Exit(0);
            }
        });
    }
}
catch (Exception e)
{
    logger.Error(e, e?.Message);
    Console.ReadKey();
    Environment.Exit(0);
}

void ShowStats(Options o, Logger l)
{
    var stats = activitiesDataStore.GetStats(o);
    l.Information($"Uploaded {stats.uploadToStrava} of {stats.allStrava} Strava activities ({stats.uploadToStrava * 100 / stats.allStrava}%)");
    l.Information($"Uploaded and updated {stats.uploadedAndUpdatedToStrava} of {stats.allStrava} Strava activities ({stats.uploadedAndUpdatedToStrava * 100 / stats.allStrava}%)");
}