namespace StravaFromEndomondoImporter.Models;

/// <summary>Endomondo JSON to Strava sync status</summary>
public class SyncStatus
{
    public int Id { get; set; }
    public string EndomondoFilePath { get; set; }
    public string EndomondoFilename { get; set; }
    public string TcxFilePath { get; set; }
    public int? DataStoreActivityId { get; set; }
    public string StravaActivityId { get; set; }
    public string CurrentStravaActivityType { get; set; }
    public string ExpectedStravaActivityType { get; set; }
    public string EndomondoActivityType { get; set; }
    public bool? NeedsUpdateInStrava { get; set; }
    public bool UpdatedInStrava { get; set; }
}