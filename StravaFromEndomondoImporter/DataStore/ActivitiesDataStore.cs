namespace StravaFromEndomondoImporter.DataStore;

public interface IActivitiesDataStore
{
    JsonFlatFileDataStore.DataStore Create(Options options);
    List<Activity> GetActivities(Options options, Status status, int? take = null);
    (int allStrava, int uploadToStrava, int uploadedAndUpdatedToStrava, int total) GetStats(Options options);
}

public class ActivitiesDataStore : IActivitiesDataStore
{
    public JsonFlatFileDataStore.DataStore Create(Options options)
    {
        return new JsonFlatFileDataStore.DataStore(Path.Combine(options.Path, "endomondo-to-strava-data-store.json"));
    }

    public List<Activity> GetActivities(Options options, Status status, int? take = null)
    {
        using var store = Create(options);
        var collection = store.GetCollection<Activity>();

        var activities = collection.AsQueryable().Where(x => !x.IsCompleted && x.Status == status);
        
        if (take.HasValue) activities = activities.Take(take.Value);

        return activities.ToList();
    }

    public (int allStrava, int uploadToStrava, int uploadedAndUpdatedToStrava, int total) GetStats(Options options)
    {
        using var ds = Create(options);
        var collection = ds.GetCollection<Activity>();

        var total = collection.Count;
        var allToProcess = collection.AsQueryable().Count(x => x.HasTrackPoints);
        var uploadedToStrava = collection.AsQueryable().Count(x => x.Status is Status.UploadSuccessful or Status.UploadAndUpdateSuccessful);
        var uploadedAndUpdatedToStrava = collection.AsQueryable().Count(x => x.HasTrackPoints && x.IsCompleted);

        return (allToProcess, uploadedToStrava, uploadedAndUpdatedToStrava, total);
    }
}