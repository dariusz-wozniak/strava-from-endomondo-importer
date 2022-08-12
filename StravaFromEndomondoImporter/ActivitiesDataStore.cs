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
}