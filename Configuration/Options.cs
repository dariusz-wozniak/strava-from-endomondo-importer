﻿using System.Diagnostics.CodeAnalysis;

namespace StravaFromEndomondoImporter.Configuration;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Options
{
    [Option('p', "path", Required = true, HelpText = "Path to input TCX files and output files (JSON data store, log files).")]
    public string Path { get; set; }

    [Option("clientid", Required = true, HelpText = "Strava client ID.")]
    public string ClientId { get; set; }

    [Option("clientsecret", Required = true, HelpText = "Strava client secret.")]
    public string ClientSecret { get; set; }

    [Option('s', "skipscan", Required = false, HelpText = "Set to true if you want to skip scanning files.")]
    public bool SkipScanning { get; set; }

    [Option('j', "jsonsync", Required = false, HelpText = "Scans for Endomondo JSON files for Strava vs. Endomondo activity type mismatches.")]
    public bool SyncWithEndomondoJsons { get; set; }
}