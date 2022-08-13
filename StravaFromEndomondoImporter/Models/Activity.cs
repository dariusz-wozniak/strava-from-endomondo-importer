namespace StravaFromEndomondoImporter.Models;

public class Activity
{
     public Activity(string filename, Status status)
     {
          Filename = filename ?? throw new ArgumentNullException(nameof(filename));
          Id = filename.GetHashCode();
          Status = status;
     }

     /// <summary>Filename</summary>
     public int Id { get;  }
     public string Filename { get; }
     public string Path { get; set; }
     public string TcxId { get; set; }
     public string TcxActivityType { get; set; }
     public DateTime? StartTime { get; set; }
     public Status Status { get; set; }
     public string StatusMessage => Status.ToString();
     public bool HasTrackPoints { get; set; }
     public string StravaActivityType { get; set; }
     public string StravaActivityId { get; set; }
     public string StravaUploadId { get; set; }
     public string StravaError { get; set; }
     public string StravaStatus { get; set; }
     public string StravaName { get; set; }
     public bool IsCompleted { get; set; }
}

public enum Status
{
     NotSet = 0,
     StartedProcessingFile = 1,
     AddedToDataStoreWithDetails = 2,
     UploadSuccessful = 3,
     UploadAndUpdateSuccessful = 4
}