using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace JobFlow.Infrastructure.Common;

public static class PollyPolicies
{
    /// <summary>
    ///     Retries up to 3 times with exponential backoff (2s, 4s, 8s).
    /// </summary>
    public static AsyncRetryPolicy DefaultRetryPolicy()
    {
        return Policy.Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, ts, retry, ctx) =>
                {
                    // TODO: Replace with your ILogger if you want
                    Console.WriteLine($"[Polly] Retry {retry} after {ts.TotalSeconds}s: {ex.Message}");
                });
    }

    /// <summary>
    ///     Opens the circuit after 5 consecutive failures, for 1 minute.
    /// </summary>
    public static AsyncCircuitBreakerPolicy DefaultCircuitBreakerPolicy()
    {
        return Policy.Handle<Exception>()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromMinutes(1),
                (ex, breakDelay) =>
                {
                    Console.WriteLine($"[Polly] Circuit open for {breakDelay.TotalSeconds}s: {ex.Message}");
                },
                () => Console.WriteLine("[Polly] Circuit closed."),
                () => Console.WriteLine("[Polly] Circuit half-open; testing…"));
    }
}