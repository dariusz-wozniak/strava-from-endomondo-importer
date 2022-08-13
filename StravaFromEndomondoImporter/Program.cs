var options = Parser.Default.ParseArguments<Options>(args).Value;

var logger = Logging.Setup(options.Path);

try
{
    // Step 1: Scan .TCX files and build/update database
    if (options.SkipScanning) logger.Information("Skipping scanning of TCX files");
    else await ActivityFileScanner.Scan(options, logger);

    // Step 2: Update ActivityType table with Endomondo data
    if (options.SyncWithEndomondoJsons) await EndomondoJsonSync.Scan(options, logger);

    ShowStats(options, logger);

    var url = $"{AuthorizeUrl}?client_id={options.ClientId}&response_type={ResponseType}&redirect_uri={RedirectUri}&scope={Scope}";
    url = url.Replace("&", "^&");

    // Step 3: Get the authorization code
    if (!BrowserRunner.RunBrowser(url)) return;
    Console.WriteLine("code=");
    var code = Console.ReadLine()?.Trim() ?? string.Empty;

    // Step 4: Authorize and fetch Bearer token
    var (accessToken, refreshToken) = await Strava.GetTokens(options, code);
    var policy = Policies.RetryPolicy(logger);

    while (true)
    {
        await policy.ExecuteAndCaptureAsync(async () =>
        {
            ShowStats(options, logger);
            
            // Step 5: Upload to Strava
            var toBeUploaded = ActivitiesDataStore.GetActivities(options, Status.AddedToDataStoreWithDetails, take: Config.BatchSize);
            logger.Information("Uploading {ActivitiesCount} activities", toBeUploaded.Count);
            foreach (var activity in toBeUploaded) await Strava.UploadActivity(accessToken, activity, logger, options);

            // Step 6: Update Strava activities (set gear ID to null, etc.)
            var toBeUpdated = ActivitiesDataStore.GetActivities(options, Status.UploadSuccessful, take: Config.BatchSize);
            logger.Information("Updating {ActivitiesCount} activities", toBeUpdated.Count);
            foreach (var activity in toBeUpdated) await Strava.UpdateActivity(accessToken, activity, logger, options);
            
            // Step 7: Scan Endomondo .JSON files and make a report of activity types that are not in sync
            bool needsToSyncActivities = false;
            if (options.SyncWithEndomondoJsons) needsToSyncActivities = await EndomondoJsonSync.Update(options, logger, accessToken);

            if (!toBeUpdated.Any() && !toBeUploaded.Any() && !needsToSyncActivities)
            {
                logger.Information("PROCESSED ALL! 🎉 - No activities to upload or update");
                ShowStats(options, logger);
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
    var (allStrava, uploadedToStrava, total) = ActivitiesDataStore.GetStats(o);
    l.Information($"Processed {uploadedToStrava} of {allStrava} Strava activities ({uploadedToStrava*100/allStrava}%)");
}