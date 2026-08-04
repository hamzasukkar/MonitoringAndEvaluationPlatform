using System.Net;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Brute-force protection on the sign-in path. There was previously none of any kind:
/// no rate limiting, and lockout was configured but never armed because every call site
/// passed lockoutOnFailure: false.
///
/// Uses its own factory so it can run against a realistic limit without throttling the
/// rest of the suite.
/// </summary>
public class RateLimitingTests : IAsyncLifetime
{
    private const int PermitLimit = 5;
    private SecurityWebApplicationFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _factory = new SecurityWebApplicationFactory();

        // After construction: the factory's constructor raises the limit so the rest of the
        // suite is not throttled. The host is built lazily on first Services access, which
        // SeedAsync triggers, so setting it here still reaches configuration.
        Environment.SetEnvironmentVariable("RateLimiting__Login__PermitLimit", PermitLimit.ToString());

        await _factory.SeedAsync();
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        // Restore the permissive limit for the shared fixture.
        Environment.SetEnvironmentVariable("RateLimiting__Login__PermitLimit", "10000");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RepeatedLoginRequests_AreEventuallyRateLimited()
    {
        var client = _factory.CreateNonRedirectingClient();

        var sawTooManyRequests = false;
        for (var i = 0; i < PermitLimit * 3; i++)
        {
            var response = await client.GetAsync("/Identity/Account/Login");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                sawTooManyRequests = true;
                break;
            }
        }

        Assert.True(sawTooManyRequests,
            $"The login endpoint served more than {PermitLimit * 3} requests without rate limiting.");
    }
}
