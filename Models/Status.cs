namespace StravaFromEndomondoImporter.Models;

public enum Status
{
    NotSet = 0,
    StartedProcessingFile = 1,
    AddedToDataStoreWithDetails = 2,
    UploadSuccessful = 3,
    UploadAndUpdateSuccessful = 4
}