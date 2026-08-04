using System.Net;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Antiforgery enforcement. Protection used to be opt-in per action and roughly 47
/// state-changing actions had opted out, including project deletion and every
/// inline-delete endpoint.
/// </summary>
[Collection("security-app")]
public class CsrfTests
{
    private readonly SecurityWebApplicationFactory _factory;

    public CsrfTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Theory]
    [InlineData("/Projects/DeleteConfirmed")]
    [InlineData("/Sectors/InlineDelete")]
    [InlineData("/Donors/InlineDelete")]
    [InlineData("/PublicSectorTypes/InlineDelete")]
    [InlineData("/Ministries/InlineDelete")]
    [InlineData("/ProjectManagers/InlineDelete")]
    [InlineData("/SuperVisors/InlineDelete")]
    [InlineData("/Location/CreateGovernorate")]
    [InlineData("/Frameworks/UpdateName")]
    [InlineData("/Outcomes/UpdateName")]
    [InlineData("/Outputs/UpdateName")]
    [InlineData("/SubOutputs/UpdateName")]
    [InlineData("/Indicators/InlineEditName")]
    [InlineData("/AuditLogs/Export")]
    public async Task StateChangingPost_WithoutToken_IsRejected(string path)
    {
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostAsync(path, new FormUrlEncodedContent(
            new Dictionary<string, string> { ["id"] = "1" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StateChangingPost_WithToken_IsNotRejectedByAntiforgery()
    {
        // The mirror image: with a token the request must get past antiforgery. Whatever the
        // action then does is not this test's concern - only that 400 is gone.
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Sectors/InlineDelete",
            new Dictionary<string, string> { ["id"] = "999999" });

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRequests_AreNotAffected()
    {
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.GetAsync("/Projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Layout_RendersTokenAndLoadsHelperScript()
    {
        // The global filter only works because every layout ships a token plus the helper
        // that attaches it to AJAX requests. If this regresses, every AJAX write breaks.
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var html = await client.GetStringAsync("/Projects");

        Assert.Contains("__RequestVerificationToken", html);
        Assert.Contains("/js/antiforgery.js", html);
    }
}

/// <summary>
/// Response headers. The application previously sent none of these.
/// </summary>
[Collection("security-app")]
public class SecurityHeaderTests
{
    private readonly SecurityWebApplicationFactory _factory;

    public SecurityHeaderTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    public async Task Response_CarriesSecurityHeader(string header, string expected)
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync("/Identity/Account/Login");

        Assert.True(response.Headers.Contains(header), $"Missing {header}.");
        Assert.Equal(expected, response.Headers.GetValues(header).First());
    }

    [Fact]
    public async Task Response_CarriesContentSecurityPolicy()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync("/Identity/Account/Login");

        var hasCsp = response.Headers.Contains("Content-Security-Policy")
                     || response.Headers.Contains("Content-Security-Policy-Report-Only");
        Assert.True(hasCsp, "No Content-Security-Policy header of either kind.");
    }

    [Fact]
    public async Task Response_CarriesPermissionsPolicy()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync("/Identity/Account/Login");

        Assert.True(response.Headers.Contains("Permissions-Policy"));
    }

    [Theory]
    [InlineData("/uploads/anything.pdf")]
    [InlineData("/uploads/measures/whatever.png")]
    [InlineData("/uploads/frameworkgoals/doc.pdf")]
    public async Task UploadedFiles_AreNotServedStatically(string path)
    {
        // UseStaticFiles runs before authentication, so anything reachable under /uploads is
        // anonymous. Uploads now live outside wwwroot and this path must 404 regardless.
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
