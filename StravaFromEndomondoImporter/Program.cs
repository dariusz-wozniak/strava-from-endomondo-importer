var options = Parser.Default.ParseArguments<Options>(args).Value;

var logFile = options.Path;
var logger = new LoggerConfiguration()
             .WriteTo.File(Path.Combine(logFile, "endomondo-to-strava.txt"), LogEventLevel.Debug)
             .CreateLogger();

const string apiHost = "https://www.strava.com/api/v3/";
const string redirectUri = "http://localhost/strava_exchange_token";
const string responseType = "code";
const string scope = "read_all,profile:read_all,activity:read_all,activity:write";
const string authorizeUrl = "https://www.strava.com/oauth/authorize";

var sports = new HashSet<string>();
int withTrack = 0;
int withNoTrack = 0;

var files = Directory.EnumerateFiles(options.Path, "*.tcx", SearchOption.AllDirectories).ToList();
foreach (var item in files.Select((value, i) => new {value, i}))
{
    var file = item.value;
    var xdoc = XDocument.Load(file);
    var jsonText = JsonConvert.SerializeXNode(xdoc);
    dynamic d = JsonConvert.DeserializeObject<ExpandoObject>(jsonText);

    var activity = d.TrainingCenterDatabase.Activities.Activity;
    var sport = ((IDictionary<string, object>)activity)["@Sport"].ToString();

    sports.Add(sport);
    
    var id = activity.Id.ToString();
    
    var track = activity.Lap.Track;
    bool hasTrack = track != null;
    
    if (hasTrack) {
        withTrack++;
    } else {
        withNoTrack++;
    }

    var lap = activity.Lap;
    DateTime startTime = DateTime.Parse(((IDictionary<string, object>)lap)["@StartTime"].ToString());
    
    logger.Information($"{id} {sport} {startTime} {hasTrack}");
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