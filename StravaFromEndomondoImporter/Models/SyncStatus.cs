namespace StravaFromEndomondoImporter.Models;

/// <summary>Endomondo JSON to Strava sync status</summary>
public class SyncStatus
{
    public SyncStatus() {}
    
    public SyncStatus(string fullPath) : this()
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
    public string CurrentStravaActivityType { get; set; }
    public string ExpectedStravaActivityType { get; set; }
    public string EndomondoActivityType { get; set; }
    public bool? NeedsUpdateInStrava { get; set; }
    public bool UpdatedInStrava { get; set; }
}