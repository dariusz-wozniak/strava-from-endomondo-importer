namespace StravaFromEndomondoImporter;

public static class ActivitiesDataStore
{
    public static DataStore Create(Options options)
    {
        return new DataStore(Path.Combine(options.Path, "endomondo-to-strava-data-store.json"));
    }
}