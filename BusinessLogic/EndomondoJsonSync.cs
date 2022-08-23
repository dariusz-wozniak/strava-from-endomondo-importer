namespace StravaFromEndomondoImporter.BusinessLogic;

public interface IEndomondoJsonSync
{
    Task Scan(Options options, Logger logger);
    Task<bool> Update(Options options, Logger logger, string accessToken);
}

public class EndomondoJsonSync : IEndomondoJsonSync
{
    private readonly IActivitiesDataStore _activitiesDataStore;
    private readonly IJsonParser _jsonParser;

    public EndomondoJsonSync(IActivitiesDataStore activitiesDataStore,
                             IJsonParser jsonParser)
    {
        _activitiesDataStore = activitiesDataStore ?? throw new ArgumentNullException(nameof(activitiesDataStore));
        _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
    }

    public async Task Scan(Options options, Logger logger)
    {
        using var syncStatusDataStore = GetSyncStatusDataStore(options);
        using var activitiesDataStore = _activitiesDataStore.Create(options);

        var syncStatuses = syncStatusDataStore.GetCollection<SyncStatus>();
        var activities = activitiesDataStore.GetCollection<Activity>().AsQueryable().ToList();

        var files = Directory.EnumerateFiles(options.Path, "*.json", SearchOption.AllDirectories)
                             .ToList();

        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);

            var activity = activities.AsQueryable()
                                     .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Filename).Equals(Path.GetFileNameWithoutExtension(file)));

            if (activity == null)
            {
                logger.Debug($"Didn't find corresponding activity {filename} in Activities Data Store, meaning - these won't be uploaded to Strava");
                await syncStatuses.DeleteManyAsync(x => x.EndomondoFilename == filename);
                continue;
            }

            var alreadyUpdated = syncStatuses.AsQueryable().Any(x => x.EndomondoFilename == filename &&
                                                                     x.UpdatedInStrava);

            if (alreadyUpdated) continue;

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

            if (activity.Status != Status.UploadAndUpdateSuccessful)
            {
                logger.Debug($"Activity is not yet updated by {nameof(StravaService)}, skipping {filename}");
                continue;
            }

            var stravaMappedActivityType = MapToStravaActivityTypeOrNull(sport);
            var needsUpdateInStrava = false;

            if (stravaMappedActivityType == null)
            {
                logger.Debug($"NO_MATCH [{filename}] Cannot match Endomondo activity type {sport} to Strava activity type {activity.StravaActivityType}");
            }
            // new - untested ⬇
            else if (!MatchesTcxActivityType(activity.TcxActivityType, stravaMappedActivityType))
            {
                logger.Information($"🎈 MISMATCH_TCX! [{filename}] Activity does not match TCX activity type {activity.TcxActivityType}. Expected {stravaMappedActivityType}");
                needsUpdateInStrava = true;
            }
            else if (!string.Equals(activity.StravaActivityType, stravaMappedActivityType, StringComparison.OrdinalIgnoreCase))
            {
                logger.Information($"👀 MISMATCH! [{filename}] Strava activity type {activity.StravaActivityType} does not match Endomondo activity type {sport}");
                needsUpdateInStrava = true;
            }
            else
            {
                logger.Debug($"MATCH [{filename}] Strava activity type {activity.StravaActivityType} match Endomondo activity type {sport}");
            }

            if (needsUpdateInStrava)
            {
                var syncStatus = new SyncStatus()
                {
                    Id = filename.GetHashCode(),
                    EndomondoFilePath = file,
                    EndomondoFilename = filename,
                    EndomondoActivityType = sport,
                    StravaActivityId = activity.StravaActivityId,
                    StravaUploadId = activity.StravaUploadId,
                    CurrentStravaActivityType = activity.StravaActivityType,
                    ExpectedStravaActivityType = stravaMappedActivityType,
                    DataStoreActivityId = activity.Id,
                    TcxFilePath = activity.Path,
                    NeedsUpdateInStrava = needsUpdateInStrava,
                };

                // Repair db:
                var duplicates = syncStatuses.AsQueryable().Where(x => x.EndomondoFilename == filename).ToList();

                if (duplicates.Any(x => x.UpdatedInStrava)) syncStatus.UpdatedInStrava = true;
                if (duplicates.Count() > 1)
                {
                    await syncStatuses.DeleteManyAsync(x => x.EndomondoFilename == filename);
                    logger.Information($"Repairing sync status for {filename}. Found {duplicates.Count()} duplicates");
                }

                await syncStatuses.ReplaceOneAsync(syncStatus.Id, syncStatus, upsert: true);
            }
        }
    }

    private bool MatchesTcxActivityType(string activityTcxActivityType, string stravaMappedActivityType)
    {
        if (string.Equals(activityTcxActivityType, Sports.Other.Tcx, StringComparison.OrdinalIgnoreCase)) return false; // Update all "Other"
        if (activityTcxActivityType == Sports.Biking.Tcx && string.Equals(stravaMappedActivityType, Sports.Biking.Strava, StringComparison.OrdinalIgnoreCase)) return true;
        if (activityTcxActivityType == Sports.Run.Tcx && string.Equals(stravaMappedActivityType, Sports.Run.Strava, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public async Task<bool> Update(Options options, Logger logger, string accessToken)
    {
        using var syncStatusDataStore = GetSyncStatusDataStore(options);
        var syncStatuses = syncStatusDataStore.GetCollection<SyncStatus>();

        var needsUpdate = syncStatuses.AsQueryable().Where(x => x.NeedsUpdateInStrava == true &&
                                                                !x.UpdatedInStrava).ToList();
        logger.Information("Found {NeedsUpdateCount} activities that needs to be updated in Strava (activity type Endomondo vs. Strava mismatched)", needsUpdate.Count);

        foreach (var syncStatus in needsUpdate.Take(BatchSizeForEndomondoActivitySync))
        {
            string activityId = string.Empty;

            if (string.IsNullOrWhiteSpace(syncStatus.StravaActivityId))
            {
                if (string.IsNullOrWhiteSpace(syncStatus.StravaUploadId)) throw new Exception($"StravaActivityId and StravaUploadId for JSON {syncStatus.EndomondoFilename} are both null");

                var upload = await Api.AppendPathSegments("uploads", syncStatus.StravaUploadId)
                                      .WithOAuthBearerToken(accessToken)
                                      .GetStringAsync();

                activityId = _jsonParser.FromJson(upload, "activity_id").ToString();
                syncStatus.StravaActivityId = activityId;
                if (string.IsNullOrWhiteSpace(activityId))
                {
                    logger.Warning($"Failed to update {syncStatus.EndomondoFilename} to Strava. Full response: {upload}");
                    continue;
                }

                var stravaActivity = await Api.AppendPathSegments("activities", activityId)
                                              .WithOAuthBearerToken(accessToken)
                                              .GetStringAsync();

                var sportType = _jsonParser.FromJson(stravaActivity, "sport_type").ToString();
                syncStatus.CurrentStravaActivityType = sportType;
                if (string.Equals(sportType, syncStatus.ExpectedStravaActivityType, StringComparison.OrdinalIgnoreCase))
                {
                    syncStatus.UpdatedInStrava = true;
                    syncStatus.NeedsUpdateInStrava = false;
                    syncStatus.StravaActivityId = activityId;
                    syncStatus.Message = "sport type matches";
                    await syncStatuses.ReplaceOneAsync(syncStatus.Id, syncStatus, upsert: true);
                    continue;
                }
            }
            else
            {
                activityId = syncStatus.StravaActivityId;
            }

            await Api.AppendPathSegments("activities", activityId)
                     .WithOAuthBearerToken(accessToken)
                     .PutJsonAsync(new
                     {
                         sport_type = syncStatus.ExpectedStravaActivityType,
                         name = $"{syncStatus.EndomondoActivityType} {NameSuffix}",
                     }).ReceiveString();

            logger.Information($"Updated activity {syncStatus.StravaActivityId} from {syncStatus.CurrentStravaActivityType} to {syncStatus.ExpectedStravaActivityType}");
            syncStatus.UpdatedInStrava = true;

            await syncStatuses.ReplaceOneAsync(syncStatus.Id, syncStatus, upsert: true);
        }

        return needsUpdate.Any();
    }

    private JsonFlatFileDataStore.DataStore GetSyncStatusDataStore(Options options)
    {
        return new JsonFlatFileDataStore.DataStore(Path.Combine(options.Path, "endomondo-to-strava-data-sync-status.json"));
    }

    private string MapToStravaActivityTypeOrNull(string endomondoSport)
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