namespace StravaFromEndomondoImporter;

public static class FileScanner
{
    public static async Task Scan(string filesWithTcxPath, Logger logger)
    {
        using var store = new DataStore(Path.Combine(filesWithTcxPath, "endomondo-to-strava-data-store.json"));
        var activities = store.GetCollection<Activity>();
        
        var files = Directory.EnumerateFiles(filesWithTcxPath, "*.tcx", SearchOption.AllDirectories).ToList();
        logger.Information("Found {FilesCount} files", files.Count);
        foreach (var item in files.Select((value, i) => new { value, i }))
        {
            var path = item.value;
            var fileName = Path.GetFileName(path);

            if (activities.AsQueryable().Any(x =>
                    x.Path == item.value && (x.IsCompleted || x.Status != Status.StartedProcessingFile)))
            {
                logger.Information("{Path} is already in store", path);
                continue;
            }

            var activity = new Activity(fileName, Status.StartedProcessingFile) { Path = path };
            await activities.InsertOneAsync(activity);

            var xdoc = XDocument.Load(path);
            var jsonText = JsonConvert.SerializeXNode(xdoc);
            dynamic d = JsonConvert.DeserializeObject<ExpandoObject>(jsonText);

            var tcxactivity = d.TrainingCenterDatabase.Activities.Activity;

            var hasTrackPoints = tcxactivity.Lap.Track != null;

            activity.TcxId = tcxactivity.Id.ToString();
            activity.StartTime = DateTime.Parse(((IDictionary<string, object>)tcxactivity.Lap)["@StartTime"].ToString());
            activity.TcxActivityType = ((IDictionary<string, object>)tcxactivity)["@Sport"].ToString();
            activity.HasTrackPoints = hasTrackPoints;
            activity.IsCompleted = !hasTrackPoints;
            activity.Status = Status.AddedToDataStoreWithDetails;

            await activities.ReplaceOneAsync(activity.Id, activity);
            logger.Information(
                $"[{item.i}]: {activity.Id} {activity.Path} {activity.TcxId} {activity.TcxActivityType} {activity.HasTrackPoints}");
        }
    }
}