namespace StravaFromEndomondoImporter.Infrastructure;

public static class BrowserRunner
{
    public static bool RunBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine("Now, browser will run. Please authorize app and copy the content of code URL query parameter (website will be HTTP Error 404 Not Found)");
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start {url}",
                UseShellExecute = true,
            });

            return true;
        }
        else
        {
            // Solution for other operating systems: https://stackoverflow.com/a/43232486/297823
            Console.WriteLine("Your OS is not supported. But, please feel free to add a new PR");
            Console.ReadKey();
            return false;
        }
    }
}