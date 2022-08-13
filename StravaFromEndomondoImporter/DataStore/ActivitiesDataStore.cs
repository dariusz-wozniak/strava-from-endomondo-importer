namespace StravaFromEndomondoImporter.DataStore;

public static class ActivitiesDataStore
{
    public static JsonFlatFileDataStore.DataStore Create(Options options)
    {
        return new JsonFlatFileDataStore.DataStore(Path.Combine(options.Path, "endomondo-to-strava-data-store.json"));
    }

    public static List<Activity> GetActivities(Options options, Status status, int? take = null)
    {
        using var store = Create(options);
        var collection = store.GetCollection<Activity>();

        var activities = collection.AsQueryable().Where(x => !x.IsCompleted && x.Status == status);
        
        if (take.HasValue) activities = activities.Take(take.Value);

        return activities.ToList();
    }

    public static (int allStrava, int uploadedToStrava, int total) GetStats(Options options)
    {
        var ds = Create(options);
        var collection = ds.GetCollection<Activity>();

        var total = collection.Count;
        var allToProcess = collection.AsQueryable().Count(x => x.HasTrackPoints);
        var uploadedToStrava = collection.AsQueryable().Count(x => x.HasTrackPoints && x.IsCompleted);

        return (allToProcess, uploadedToStrava, total);
    }
}