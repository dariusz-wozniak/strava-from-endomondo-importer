namespace StravaFromEndomondoImporter.BusinessLogic;

public static class EndomondoJsonSync
{
    public static async Task Scan(Options options, Logger logger)
    {
        using var syncStatusDataStore = GetSyncStatusDataStore(options);
        using var activitiesDataStore = ActivitiesDataStore.Create(options);

        var syncStatuses = syncStatusDataStore.GetCollection<SyncStatus>();
        var activities = activitiesDataStore.GetCollection<Activity>().AsQueryable().ToList();

        var files = Directory.EnumerateFiles(options.Path, "*.json", SearchOption.AllDirectories)
                             .ToList();

        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            
            string sport;

            try
            {
                var content = await File.ReadAllTextAsync(file);
                var array = JArray.Parse(content);
                var children = array.Children<JObject>();
                var props = children.Properties();
                var prop = props.FirstOrDefault(x =>
                    string.Equals(x.Name, "sport", StringComparison.OrdinalIgnoreCase));
                sport = prop?.Value.ToString();
            }
            catch (Exception e) when (e is RuntimeBinderException or JsonReaderException)
            {
                logger.Error(e, $"Error parsing JSON file {file}");
                continue;
            }

            var activity = activities.AsQueryable()
                                     .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Filename).Equals(Path.GetFileNameWithoutExtension(file)));

            if (activity == null)
            {
                logger.Information($"Didn't find corresponding activity {filename} in Activities Data Store, meaning - these won't be uploaded to Strava");
                continue;
            }
            
            var stravaMappedActivityType = MapToStravaActivityTypeOrNull(sport);
            var needsUpdateInStrava = false;
            
            if (stravaMappedActivityType == null)
            {
                logger.Information($"NO_MATCH [{filename}] Cannot match Endomondo activity type {sport} to Strava activity type {activity.StravaActivityType}");
            }
            else if (string.IsNullOrWhiteSpace(activity.StravaActivityType))
            {
                logger.Information($"NOT_ON_STRAVA [{filename}] Activity not found on Strava");
            }
            else if (!string.Equals(activity.StravaActivityType, stravaMappedActivityType, StringComparison.OrdinalIgnoreCase))
            {
                logger.Information($"👀 MISMATCH! [{filename}] Strava activity type {activity.StravaActivityType} does not match Endomondo activity type {sport}");
                needsUpdateInStrava = true;
            }
            else
            {
                logger.Information($"MATCH [{filename}] Strava activity type {activity.StravaActivityType} match Endomondo activity type {sport}");
            }

            if (needsUpdateInStrava)
            {
                var syncStatus = new SyncStatus()
                {
                    Id = file.GetHashCode(),
                    EndomondoFilePath = file,
                    EndomondoFilename = filename,
                    EndomondoActivityType = sport,
                    StravaActivityId = activity.StravaActivityId,
                    CurrentStravaActivityType = activity.StravaActivityType,
                    ExpectedStravaActivityType = stravaMappedActivityType,
                    DataStoreActivityId = activity.Id,
                    TcxFilePath = activity.Path,
                    NeedsUpdateInStrava = needsUpdateInStrava,
                };

                await syncStatuses.ReplaceOneAsync(syncStatus.Id, syncStatus, upsert: true);
            }
        }
    }

    public static async Task<bool> Update(Options options, Logger logger, string accessToken)
    {
        using var syncStatusDataStore = GetSyncStatusDataStore(options);
        var syncStatuses = syncStatusDataStore.GetCollection<SyncStatus>();

        var needsUpdate = syncStatuses.AsQueryable().Where(x => x.NeedsUpdateInStrava == true).ToList();
        logger.Information("Found {NeedsUpdateCount} activities that needs to be updated in Strava (clear gear ID, etc.)", needsUpdate.Count);
        
        foreach (var syncStatus in needsUpdate.Take(BatchSizeForEndomondoActivitySync))
        {
            var update = await Api.AppendPathSegments("activities", syncStatus.StravaActivityId)
                                  .WithOAuthBearerToken(accessToken)
                                  .PutJsonAsync(new
                                  {
                                      sport_type = syncStatus.ExpectedStravaActivityType,
                                      name = $"{syncStatus.EndomondoActivityType} {NameSuffix}",
                                  })
                                  .ReceiveString();
            
            logger.Information($"Updated activity {syncStatus.StravaActivityId} from {syncStatus.CurrentStravaActivityType} to {syncStatus.ExpectedStravaActivityType}");
            syncStatus.UpdatedInStrava = true;
            
            await syncStatuses.ReplaceOneAsync(syncStatus.Id, syncStatus, upsert: true);
        }

        return needsUpdate.Any();
    }

    private static JsonFlatFileDataStore.DataStore GetSyncStatusDataStore(Options options)
    {
        return new JsonFlatFileDataStore.DataStore(Path.Combine(options.Path, "endomondo-to-strava-data-sync-status.json"));
    }

    private static string MapToStravaActivityTypeOrNull(string endomondoSport)
    {
        if (string.IsNullOrWhiteSpace(endomondoSport)) return null;

        return endomondoSport.ToLowerInvariant() switch
        {
            // ↓ lower-cased strings here
            "running" => Sports.Run.Strava,
            "cycling_sport" => Sports.Biking.Strava,
            "cycling_transportation" => Sports.Biking.Strava,
            "mountain_biking" => Sports.Biking.Strava,
            "walking" => Sports.Walk.Strava,
            "hiking" => Sports.Hike.Strava,
            "aerobics" => Sports.Other.Strava,
            "badminton" => Sports.Other.Strava,
            "basketball" => Sports.Other.Strava,
            "beach_volley" => Sports.Other.Strava,
            "climbing" => "RockClimbing",
            "cross_training" => "Crossfit",
            "crossfit" => "Crossfit",
            "dancing" => Sports.Other.Strava,
            "ice_skating" => "IceSkate",
            "kayaking" => "Kayaking",
            "riding" => Sports.Other.Strava,
            "rowing" => "Rowing",
            "rowing_indoor" => "Rowing",
            "sailing" => "Sail",
            "skiing_cross_country" => "BackcountrySki",
            "skiing_downhill" => "AlpineSki",
            "soccer" => Sports.Other.Strava,
            "swimming" => "Swim",
            "tennis" => Sports.Other.Strava,
            "treadmill_running" => Sports.Run.Strava,
            "treadmill_walking" => Sports.Walk.Strava,
            _ => null
        };
    }
}