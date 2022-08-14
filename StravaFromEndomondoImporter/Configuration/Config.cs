namespace StravaFromEndomondoImporter.Configuration;

public static class Config
{
    public static int BatchSizeForUploading => 80;
    public static int BatchSizeForUpdating => 10;
    public static int BatchSizeForEndomondoActivitySync => 10;
    public static int RetryCount => 10;
}