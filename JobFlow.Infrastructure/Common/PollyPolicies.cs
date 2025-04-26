using System;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;

namespace JobFlow.Infrastructure.Common
{
    public static class PollyPolicies
    {
        /// <summary>
        /// Retries up to 3 times with exponential backoff (2s, 4s, 8s).
        /// </summary>
        public static AsyncRetryPolicy DefaultRetryPolicy() =>
            Policy.Handle<Exception>()
                  .WaitAndRetryAsync(
                      retryCount: 3,
                      sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                      onRetry: (ex, ts, retry, ctx) =>
                      {
                          // TODO: Replace with your ILogger if you want
                          Console.WriteLine($"[Polly] Retry {retry} after {ts.TotalSeconds}s: {ex.Message}");
                      });

        /// <summary>
        /// Opens the circuit after 5 consecutive failures, for 1 minute.
        /// </summary>
        public static AsyncCircuitBreakerPolicy DefaultCircuitBreakerPolicy() =>
            Policy.Handle<Exception>()
                  .CircuitBreakerAsync(
                      exceptionsAllowedBeforeBreaking: 5,
                      durationOfBreak: TimeSpan.FromMinutes(1),
                      onBreak: (ex, breakDelay) =>
                      {
                          Console.WriteLine($"[Polly] Circuit open for {breakDelay.TotalSeconds}s: {ex.Message}");
                      },
                      onReset: () => Console.WriteLine("[Polly] Circuit closed."),
                      onHalfOpen: () => Console.WriteLine("[Polly] Circuit half-open; testing…"));
    }
}
