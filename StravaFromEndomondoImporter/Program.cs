var options = Parser.Default.ParseArguments<Options>(args).Value;
var logger = Logging.Setup(options.Path);

if (!options.SkipScanning) await FileScanner.Scan(options.Path, logger);

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