var options = Parser.Default.ParseArguments<Options>(args).Value;

var logger = Logging.Setup(options.Path);
var store = new DataStore(Path.Combine(options.Path, "endomondo-to-strava-data-store.json"));
var activities = store.GetCollection<Activity>();

var files = Directory.EnumerateFiles(options.Path, "*.tcx", SearchOption.AllDirectories).ToList();
logger.Information("Found {FilesCount} files", files.Count);
foreach (var item in files.Select((value, i) => new {value, i}))
{
    var path = item.value;
    var fileName = Path.GetFileName(path);

    if (activities.AsQueryable().Any(x => x.Path == item.value && (x.IsCompleted || x.Status != Status.StartedProcessingFile)))
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
    logger.Information($"[{item.i}]: {activity.Id} {activity.Path} {activity.TcxId} {activity.TcxActivityType} {activity.HasTrackPoints}");
}

Environment.Exit(0);

// for later:
/*

var url = $"{authorizeUrl}?client_id={options.ClientId}&response_type={responseType}&redirect_uri={redirectUri}&scope={scope}";
url = url.Replace("&", "^&");

if (!BrowserRunner.RunBrowser(url)) return;

Console.WriteLine("code=");

var code = Console.ReadKey();

string tcx = files.First();

var tokenRs = await apiHost.AppendPathSegments("oauth", "token")
                        .PostUrlEncodedAsync(new Dictionary<string, string>
                        {
                            { "client_id", options.ClientId },
                            { "client_secret", options.ClientSecret },
                            { "code", code },
                            { "grant_type", "authorization_code" },
                        })
                        .ReceiveString();

var accessToken = JObject.Parse(tokenRs)["access_token"].ToString();
var refreshToken = JObject.Parse(tokenRs)["refresh_token"].ToString();

var rs = await apiHost.AppendPathSegments("uploads")
                   .WithOAuthBearerToken(accessToken)
                   .AllowAnyHttpStatus()
                   .PostMultipartAsync(content => 
                       content.AddFile("file", tcx)
                              .AddString("name", "👀 Uploaded from api")
                              .AddString("data_type", "tcx")
                   )
                   .ReceiveString();

var id = JObject.Parse(rs)["id"].ToString();

// TODO:
// Get uploads
// Update (e.g. gear)

var rsPut = await apiHost.AppendPathSegments("activities", id)
                      .WithOAuthBearerToken(accessToken)
                      .AllowAnyHttpStatus()
                      .PutJsonAsync(new
                      {
                          hide_from_home = true
                      })
                      .ReceiveString();


Console.ReadKey();

*/