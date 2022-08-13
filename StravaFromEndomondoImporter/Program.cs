﻿using StravaFromEndomondoImporter.BusinessLogic;
using StravaFromEndomondoImporter.Configuration;
using StravaFromEndomondoImporter.DataStore;
using StravaFromEndomondoImporter.Infrastructure;
using StravaFromEndomondoImporter.Models;

var options = Parser.Default.ParseArguments<Options>(args).Value;

var logger = Logging.Setup(options.Path);

try
{
    if (options.SkipScanning) logger.Information("Skipping scanning");
    else await ActivityFileScanner.Scan(options, logger);

    if (options.SyncWithEndomondoJsons) EndomondoJsonSync.Scan(options, logger);

    ShowStats(options, logger);

    var url = $"{AuthorizeUrl}?client_id={options.ClientId}&response_type={ResponseType}&redirect_uri={RedirectUri}&scope={Scope}";
    url = url.Replace("&", "^&");

    if (!BrowserRunner.RunBrowser(url)) return;
    Console.WriteLine("code=");
    var code = Console.ReadLine()?.Trim() ?? string.Empty;

    var (accessToken, refreshToken) = await Strava.GetTokens(options, code);
    var policy = Policies.RetryPolicy(logger);

    while (true)
    {
        await policy.ExecuteAndCaptureAsync(async () =>
        {
            // Upload:
            var toBeUploaded = ActivitiesDataStore.GetActivities(options, Status.AddedToDataStoreWithDetails, take: Config.BatchSize);
            logger.Information("Uploading {ActivitiesCount} activities", toBeUploaded.Count);
            foreach (var activity in toBeUploaded) await Strava.UploadActivity(accessToken, activity, logger, options);

            // Update:
            var toBeUpdated =
                ActivitiesDataStore.GetActivities(options, Status.UploadSuccessful, take: Config.BatchSize);
            logger.Information("Updating {ActivitiesCount} activities", toBeUpdated.Count);
            foreach (var activity in toBeUpdated) await Strava.UpdateActivity(accessToken, activity, logger, options);

            if (!toBeUpdated.Any() && !toBeUploaded.Any())
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

Console.ReadKey();

void ShowStats(Options o, Logger l)
{
    var (processed, uploaded, total) = ActivitiesDataStore.GetStats(o);
    l.Information("Processed {Processed} of {Total} activities ({Percentage}%). Uploaded so far: {Uploaded}", processed, total, processed * 100 / total, uploaded);
}