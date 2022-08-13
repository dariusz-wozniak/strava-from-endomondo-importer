namespace StravaFromEndomondoImporter;

public static class ActivitiesDataStore
{
    public static DataStore Create(Options options)
    {
        return new DataStore(Path.Combine(options.Path, "endomondo-to-strava-data-store.json"));
    }

    public static List<Activity> GetActivities(Options options, Status status, int? take = null)
    {
        using var store = Create(options);
        var collection = store.GetCollection<Activity>();

        var activities = collection.AsQueryable().Where(x => !x.IsCompleted && x.Status == status);
        
        if (take.HasValue) activities = activities.Take(take.Value);

        return activities.ToList();
    }

    public static (int processed, int uploaded, int total) GetStats(Options options)
    {
        var ds = Create(options);
        var collection = ds.GetCollection<Activity>();

        var total = collection.Count;
        var processed = collection.AsQueryable().Count(x => x.IsCompleted);
        var uploaded = collection.AsQueryable().Count(x => x.Status == Status.UploadSuccessful || x.Status == Status.UploadAndUpdateSuccessful);

        return (processed, uploaded, total);
    }
}