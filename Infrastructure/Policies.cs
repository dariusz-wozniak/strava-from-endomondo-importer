﻿using Polly.Retry;

namespace StravaFromEndomondoImporter.Infrastructure;

public static class Policies
{
    public static AsyncRetryPolicy RetryPolicy(Logger logger)
    {
        // Strava: The default rate limit allows 100 requests every 15 minutes, with up to 1,000 requests per day.
        var policy = Policy.Handle<FlurlHttpException>(e => e.StatusCode == (int)HttpStatusCode.TooManyRequests)
                           .WaitAndRetryAsync(retryCount: RetryCount, retryAttempt => TimeSpan.FromMinutes(15),
                               (exception, span) =>
                               {
                                   logger.Warning($"Retrying in {span.TotalMinutes} minutes... Exception: {exception?.Message}");
                               });

        return policy;
    }
}