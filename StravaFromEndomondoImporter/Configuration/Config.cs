namespace StravaFromEndomondoImporter.Configuration;

public static class Config
{
    public static int BatchSizeForUploading => 50;
    public static int BatchSizeForUpdating => 10;
    public static int BatchSizeForEndomondoActivitySync => 50;
    public static int RetryCount => 20;
}