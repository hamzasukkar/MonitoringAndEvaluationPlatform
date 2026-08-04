using System.Net;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Boots the application once for the whole class - startup is the expensive part.
/// </summary>
public class SecurityAppFixture : IAsyncLifetime
{
    public SecurityWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new SecurityWebApplicationFactory();
        await Factory.SeedAsync();
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("security-app")]
public class SecurityAppCollection : ICollectionFixture<SecurityAppFixture>;

[Collection("security-app")]
public class AuthenticationTests
{
    private readonly SecurityWebApplicationFactory _factory;

    public AuthenticationTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Theory]
    [InlineData("/Projects")]
    [InlineData("/Frameworks")]
    [InlineData("/Measures")]
    [InlineData("/Plans")]
    [InlineData("/ActionPlans")]
    [InlineData("/Donors")]
    [InlineData("/Sectors")]
    [InlineData("/Location")]
    [InlineData("/PublicSectorTypes")]
    [InlineData("/Admin/Users")]
    [InlineData("/DataManagement")]
    [InlineData("/AuditLogs")]
    public async Task AnonymousRequest_IsNotServed(string path)
    {
        // Before the fallback policy, ten of these controllers were reachable by any
        // authenticated principal and were guarded only by a hand-rolled middleware.
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized });
    }

    [Theory]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/ForgotPassword")]
    public async Task AnonymousRequest_ToSignInPages_IsAllowed(string path)
    {
        // These pages lost their [AllowAnonymous] at some point. With a fallback policy in
        // place that turns into an infinite login redirect, and it is also what made
        // self-service password reset unreachable.
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousRequest_ToRegister_IsRejected()
    {
        // Self-registration used to be whitelisted and auto-signed-in the new account.
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.GetAsync("/Identity/Account/Register");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedNonAdmin_CannotReachRegister()
    {
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.MinistryAUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.GetAsync("/Identity/Account/Register");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidCredentials_SignIn()
    {
        // Guards the fix for the login InputModel: a [Required] Email field the form never
        // rendered made ModelState permanently invalid, which is why the handler had been
        // written `if (ModelState.IsValid || true)`.
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.LoginAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task InvalidPassword_DoesNotSignIn()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.LoginAsync(SecurityWebApplicationFactory.AdminUserName, "WrongPassword!123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        // Generic message: must not reveal whether the username exists.
        Assert.Contains("Invalid login attempt", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPost_WithoutAntiforgeryToken_IsRejected()
    {
        var client = _factory.CreateNonRedirectingClient();

        var response = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Input.UserName"] = SecurityWebApplicationFactory.AdminUserName,
                ["Input.Password"] = SecurityWebApplicationFactory.TestPassword
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
