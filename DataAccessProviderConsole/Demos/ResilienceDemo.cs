using DataAccessProvider.Core.Resilience;
using System.Diagnostics;

namespace DataAccessProviderConsole.Demos;

public static class ResilienceDemo
{
    public static async Task RunAsync()
    {
        var policy = new BasicResiliencePolicy(maxRetries: 3, perAttemptTimeout: TimeSpan.FromSeconds(1));

        var unstableResult = await policy.ExecuteAsync<int>(async ct =>
        {
            const int failUntilAttempt = 2;

            var currentAttempt = UnstableOperationState.NextAttempt();

            Debug.WriteLine($"[ResilienceTest] Attempt {currentAttempt}");

            await Task.Delay(600, ct);

            if (currentAttempt <= failUntilAttempt)
            {
                throw new InvalidOperationException($"Simulated transient failure on attempt {currentAttempt}");
            }

            return currentAttempt;
        });

        Console.WriteLine($"\n[ResilienceTest] Completed with attempt: {unstableResult}");
    }

    private static class UnstableOperationState
    {
        private static int _attempt;

        public static int NextAttempt()
        {
            return Interlocked.Increment(ref _attempt);
        }
    }
}