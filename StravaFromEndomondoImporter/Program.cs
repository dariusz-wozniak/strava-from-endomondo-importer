using System.Globalization;
using System.Xml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

var options = Parser.Default.ParseArguments<Options>(args).Value;

const string host = "https://www.strava.com/api/v3/";
const string redirectUri = "http://localhost/strava_exchange_token";
const string responseType = "code";
const string scope = "read_all,profile:read_all,activity:read_all,activity:write";
const string authorizeUrl = "https://www.strava.com/oauth/authorize";

var url = $"{authorizeUrl}?client_id={options.ClientId}&response_type={responseType}&redirect_uri={redirectUri}&scope={scope}";
url = url.Replace("&", "^&");

Console.WriteLine("Now, browser will run. Please authorize app and copy the content of code URL query parameter (website will be HTTP Error 404 Not Found)");

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Process.Start(new ProcessStartInfo
    {
        FileName = "cmd.exe",
        Arguments = $"/c start {url}",
        UseShellExecute = true,
    });
}
else
{
    // Solution for other OSs: https://stackoverflow.com/a/43232486/297823
    Console.WriteLine("Your OS is not supported. But, please feel free to add a new PR");
    Console.ReadKey();
    return;
}

Console.WriteLine("code=");

var code = Console.ReadLine();

var files = Directory.EnumerateFiles(options.Path, "*.tcx", SearchOption.AllDirectories).ToList();
foreach (var file in files)
{
    var doc = new XmlDocument();
    doc.Load(file);
}

string tcx = files.First();

var tokenRs = await host.AppendPathSegments("oauth", "token")
                        .PostUrlEncodedAsync(new Dictionary<string, string>
                        {
                            { "client_id", options.ClientId },
                            { "client_secret", options.ClientSecret },
                            { "code", code },
                            { "grant_type", "authorization_code" },
                        })
                        .ReceiveString();

var jObject = JObject.Parse(tokenRs);

var accessToken = jObject["access_token"].ToString();
var refreshToken = jObject["refresh_token"].ToString();

// string accessToken = ((IEnumerable<KeyValuePair<string, object>>)tokenRs).First(x => x.Key == "access_token").Value.ToString();
// string refreshToken = ((IEnumerable<KeyValuePair<string, object>>)tokenRs).First(x => x.Key == "refresh_token").Value.ToString();

// var rs = await host.AppendPathSegments("activities", 7607224570)
//                    .WithOAuthBearerToken(accessToken)
//                    .GetStringAsync();

// var rs = await host.AppendPathSegments("activities")
//                    .WithOAuthBearerToken(accessToken)
//                    .AllowAnyHttpStatus()
//                    .PostUrlEncodedAsync(new Dictionary<string, object>
//                    {
//                        { "name", "😎 test from api" },
//                        { "type", "Walk"  }, // https://developers.strava.com/docs/reference/#api-models-ActivityType
//                        { "sport_type", "Walk"  }, // https://developers.strava.com/docs/reference/#api-models-SportType
//                        { "start_date_local", DateTime.Now.AddMilliseconds(-1).ToString("s", CultureInfo.InvariantCulture) },
//                        { "elapsed_time", 2 }, // In seconds
//                        { "hide_from_home", true },
//
//                    })
//                    .ReceiveString();

var rs = await host.AppendPathSegments("uploads")
                   .WithOAuthBearerToken(accessToken)
                   .AllowAnyHttpStatus()
                   .PostMultipartAsync(content => 
                       content.AddFile("file", tcx)
                              .AddString("name", "👀 Uploaded from api")
                              .AddString("data_type", "tcx")
                   )
                   .ReceiveString();

var jrs = JObject.Parse(rs);

var id = jrs["id"].ToString();

var rsPut = await host.AppendPathSegments("activities", id)
                      .WithOAuthBearerToken(accessToken)
                      .AllowAnyHttpStatus()
                      .PutJsonAsync(new
                      {
                          hide_from_home = true
                      })
                      .ReceiveString();

Console.WriteLine(rs);

Console.ReadKey();

// https://developers.strava.com/docs/authentication/