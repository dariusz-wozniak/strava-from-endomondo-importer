using Newtonsoft.Json.Linq;

var options = Parser.Default.ParseArguments<Options>(args).Value;

const string host = "https://www.strava.com/api/v3/";
const string redirectUri = "http://localhost/strava_exchange_token";
const string responseType = "code";
const string scope = "read_all,profile:read_all,activity:read_all";
const string authorizeUrl = "https://www.strava.com/oauth/authorize";

var url =
    $"{authorizeUrl}?client_id={options.ClientId}&response_type={responseType}&redirect_uri={redirectUri}&scope={scope}";
url = url.Replace("&", "^&");

Console.WriteLine(
    "Now, browser will run. Please authorize app and copy the content of code URL query parameter (website will be HTTP Error 404 Not Found)");

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

var rs = await host.AppendPathSegments("activities", 7607224570)
                   .WithOAuthBearerToken(accessToken)
                   .GetStringAsync();

Console.WriteLine(rs);

Console.ReadKey();

// https://developers.strava.com/docs/authentication/