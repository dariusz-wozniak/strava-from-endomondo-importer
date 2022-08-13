namespace StravaFromEndomondoImporter.Models;

public static class Sports
{
    public static class Walk
    {
        public const string Strava = "Walk";
        public const string Endomondo = "WALKING";
    }

    public static class Run
    {
        public const string Strava = "Run";
        public const string Endomondo = "RUNNING";
        public const string Tcx = "Running";
    }
    
    public static class Biking
    {
        public const string Strava = "Ride";
        public const string Endomondo = "CYCLING_SPORT";
        public const string Tcx = "Biking";
    }

    public static class Other
    {
        public const string Strava = "Workout";
        public const string Endomondo = "Workout";
        public const string Tcx = "Other";
    }

    public static class Hike
    {
        public const string Strava = "Hike";
    }
}