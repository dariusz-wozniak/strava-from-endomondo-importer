using Polly.Retry;
using StravaFromEndomondoImporter.Configuration;

namespace StravaFromEndomondoImporter.Infrastructure;

public static class Policies
{
    public static AsyncRetryPolicy RetryPolicy(Logger logger)
    {
        // The default rate limit allows 100 requests every 15 minutes, with up to 1,000 requests per day.
        var policy = Policy.Handle<FlurlHttpException>(e => e.StatusCode == (int)HttpStatusCode.TooManyRequests)
                           .WaitAndRetryAsync(retryCount: Config.RetryCount, retryAttempt => TimeSpan.FromMinutes(retryAttempt * 2),
                               (exception, span) =>
                               {
                                   logger.Warning($"Retrying in {span.TotalMinutes} minutes... Exception: {exception?.Message}");
                               });

        return policy;
    }
}