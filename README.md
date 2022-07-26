﻿<img height="150" src="logo.png" width="150"/>

**Strava-From-Endomondo-Importer** is the console application that imports data from Endomondo files to your Strava profile.

That's a quick-and-dirty application that I wrote for myself as a single run app, but decided to publish it, so you can use that for your import. It does support a lot and works gently with thousands a lot, but be aware about its limitations (see the Limitations section).

# 💾 Download

Check for the releases here: https://github.com/dariusz-wozniak/strava-from-endomondo-importer/releases

# 👀 Features

The main features of that lovely app are:

- Uploading activity does not affect Home Page, so it is not visible at the top of all activities (at Strava Home Page).
- Supports big amount of files. My Endomondo archive contained almost 4000 files and it works smoothly. The only limit is a daily API limit of 1000 calls. But, you can start the job the next day (clock resets at 0 UTC).
- Supports importing *.TCX files and sync with the Endomondo JSON files (optional).
- Supports custom mapping between activties.
- Supports retrying, so if you have reached maximum threshold of API calls in 15 minutes, it will make several retries.
- Build the small local database with the current state, so it continues the job after restarting the app.

# 🚴‍♂️ Usage

Run the `StravaFromEndomondoImporter.exe` with the following arguments:

```
  -p, --path        Required. Path to input TCX files and output files (JSON data store, log files).

  --clientid        Required. Strava client ID.

  --clientsecret    Required. Strava client secret.

  -s, --skipscan    Set to true if you want to skip scanning files.

  -j, --jsonsync    Scans for Endomondo JSON files for Strava vs. Endomondo activity type mismatches.

  --help            Display this help screen.

  --version         Display version information.
```

## Get Client ID and Client Secret

In order to get your client ID and client secret, you need to register an application at [Strava](https://www.strava.com/settings/api).

1. For the form:
   - Application Name: _type whatever you want_
   - Category: **Data Importer**
   - Website: _any of website with valid URL_
   - Authorization Callback Domain: **localhost**
2. Next, you will need to provide any of image in the "Update App Icon" form.

3. And finally, you will get "My API Application" with all the details you will need, i.e. Client ID and Client Secret.

## Application flow

The app uses the following steps to import data:

1. Scan for *.TCX files in the path.
   1. Files state are being maintained in the local JSON data store (`endomondo-to-strava-data-store.json`).
   2. If the `--skipscan` or `-s` option is set, the app will not scan for .TCX files. That can be useful if the scan process was already done.
2. The browser should be opened with Authorize option. Click on the Authorize button and then you will be redirected to the 404 localhost page. Copy the code from the page and paste it into the command line and then press Enter.
   1. For example, for URL:
   
   ```
   http://localhost:80/strava_exchange_token?state=&code=123abcdef&scope=read,activity:write,activity:read_all,profile:read_all,read_all
   ```
   
   Copy the `123abcdef` part and paste it into the command line.

3. The app will try to process importing activities to Strava in the 3 steps:
    1. Upload activity to Strava.
    2. Update activity in the Strava.
       1. Adjust `sport_type` basing on the *.TCX file (if needed).
       2. Set `gear_id` to null. This is because `gear_id` is being set to default one.
       3. Set `hide_from_home` to true. 
    3. Scan for Endomondo-specific *.JSON and adjust activity types.
       1. If there are any needs to update then it will happen.  
       2. The state is maintained under its JSON data store (`endomondo-to-strava-data-sync-status.json`).

## Logging

The app is very chatty and you may find it useful to see the logs. See `-p` or `--path` option for the path to the log files.

# ⚠ Limitations

## API rate limit

Strava API has a rate limit per API application:

> The default rate limit allows 100 requests every 15 minutes, with up to 1,000 requests per day.

The app retries failed requests after 15 minutes, but doesn't look into daily limit, so you might need to close application manually or wait until throttling will be completed. 

## Access token lifetime

Access token lives for 6 hours and the app doesn't care about refreshing it. If the run is above 6 hrs., restart the app.

## Operating systems

The app run on Windows only.

If you want to run it on another system, you need to modify the code. See `BrowserRunner.cs` as the first class to modify -- there is a hint on how to modify it for Linux and Mac. If you successfully run the app on another OS, please do not hesitate to create a new Pull Request 😎

## Activity types

Mappings of activity types are personalized and adjusted to my needs. You might modify them as well.

See the following methods to modify:
* `StravaService.Map`
* `EndomondoJsonSync.MapToStravaActivityTypeOrNull`

# ⚖ License

The app is published as-is. The authors do not take any responsibility for losing or altering any of data.

See the `LICENSE` file.

# 🖼 Logo

Logo has been generated by DALL·E 2.
