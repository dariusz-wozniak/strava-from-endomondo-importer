using StravaFromEndomondoImporter.DataStore;
using StravaFromEndomondoImporter.Models;

namespace StravaFromEndomondoImporter.BusinessLogic;

public static class EndomondoJsonSync
{
    public static void Scan(Options options, Logger logger)
    {
        using var syncStatusDataStore =
            new JsonFlatFileDataStore.DataStore(Path.Combine(options.Path,
                "endomondo-to-strava-data-sync-status.json"));
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
                var content = File.ReadAllText(file);
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
                logger.Information($"Didn't find corresponding activity {filename} in Activities Data Store, meaning - these might not be uploaded to Strava");
                continue;
            }
            
            var mappedActivityType = MapToStravaActivityTypeOrNull(sport);

            bool needsUpdateInStrava = false;
            
            if (mappedActivityType == null)
            {
                logger.Information($"NO_MATCH [{filename}] Cannot match Endomondo activity type {sport} to Strava activity type {activity.StravaActivityType}");
            }
            else if (string.IsNullOrWhiteSpace(activity.StravaActivityType))
            {
                logger.Information($"NOT_ON_STRAVA [{filename}] Activity not yet uploaded to Strava");
            }
            else if (!string.Equals(activity.StravaActivityType, mappedActivityType, StringComparison.OrdinalIgnoreCase))
            {
                logger.Information($"👀 MISMATCH! [{filename}] Strava activity type {activity.StravaActivityType} does not match Endomondo activity type {sport}");
                needsUpdateInStrava = true;
            }
            else
            {
                logger.Information($"MATCH [{filename}] Strava activity type {activity.StravaActivityType} match Endomondo activity type {sport}");
            }

            var syncStatus = new SyncStatus(file)
            {
                EndomondoActivityType = sport,
                StravaActivityId = activity.StravaActivityId,
                StravaActivityType = activity.StravaActivityType,
                DataStoreActivityId = activity.Id,
                TcxFilePath = activity.Path,
                NeedsUpdateInStrava = needsUpdateInStrava,
            };
        }
    }

    private static string MapToStravaActivityTypeOrNull(string endomondoSport)
    {
        if (string.IsNullOrWhiteSpace(endomondoSport)) return null;

        return endomondoSport.ToLowerInvariant() switch
        {
            "running" => Sports.Run.Strava,
            "cycling_sport" => Sports.Biking.Strava,
            "cycling_transportation" => Sports.Biking.Strava,
            "mountain_biking" => Sports.Biking.Strava,
            "walking" => Sports.Walk.Strava,
            "hiking" => Sports.Hike.Strava,
            _ => null
        };
    }
}

/// <summary>Endomondo JSON to Strava sync status</summary>
public class SyncStatus
{
    public SyncStatus(string fullPath)
    {
        EndomondoFilePath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
        EndomondoFilename = Path.GetFileName(fullPath);
        Id = EndomondoFilename.GetHashCode();
    }

    public int Id { get; }
    public string EndomondoFilePath { get; }
    public string EndomondoFilename { get; }
    public string TcxFilePath { get; set; }
    public int? DataStoreActivityId { get; set; }
    public string StravaActivityId { get; set; }
    public string StravaActivityType { get; set; }
    public string EndomondoActivityType { get; set; }
    public bool? NeedsUpdateInStrava { get; set; }
    public bool UpdatedInStrava { get; set; }
}