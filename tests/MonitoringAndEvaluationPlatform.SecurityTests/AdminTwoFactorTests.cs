using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Administrative reset of a user's second factor.
///
/// This endpoint strips a security control off another account, so it is exactly what an
/// attacker holding a stolen administrator session would reach for. It must be unreachable
/// without the administrator role and without an antiforgery token, and it must never work
/// in reverse - an administrator cannot enrol a factor for someone else.
/// </summary>
[Collection("security-app")]
public class AdminTwoFactorTests
{
    private readonly SecurityWebApplicationFactory _factory;

    public AdminTwoFactorTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Administrator_CanResetAnotherUsersTwoFactor()
    {
        var userName = await CreateUserWithTwoFactorAsync("tfa_reset_target");
        var before = await GetStateAsync(userName);
        Assert.True(before.TwoFactorEnabled);
        Assert.NotNull(before.AuthenticatorKey);

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Admin/ResetTwoFactor",
            new Dictionary<string, string> { ["id"] = before.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\":true", await response.Content.ReadAsStringAsync());

        var after = await GetStateAsync(userName);
        Assert.False(after.TwoFactorEnabled);

        // The old authenticator must stop working, not merely go unenforced - otherwise a
        // stolen device still produces valid codes the moment 2FA is switched back on.
        Assert.NotEqual(before.AuthenticatorKey, after.AuthenticatorKey);

        // And any session already issued for that account must be invalidated.
        Assert.NotEqual(before.SecurityStamp, after.SecurityStamp);
    }

    [Fact]
    public async Task Reset_OnUserWithoutTwoFactor_IsRejected()
    {
        var state = await GetStateAsync(SecurityWebApplicationFactory.ReaderUserName);
        Assert.False(state.TwoFactorEnabled);

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Admin/ResetTwoFactor",
            new Dictionary<string, string> { ["id"] = state.Id });

        // A no-op must report itself as one rather than claiming success, so an operator is
        // never told a factor was cleared when none existed.
        Assert.Contains("\"success\":false", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NonAdministrator_CannotResetTwoFactor()
    {
        var target = await GetStateAsync(SecurityWebApplicationFactory.MinistryBUserName);

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.MinistryAUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Admin/ResetTwoFactor",
            new Dictionary<string, string> { ["id"] = target.Id });

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reset_WithoutAntiforgeryToken_IsRejected()
    {
        var target = await GetStateAsync(SecurityWebApplicationFactory.ReaderUserName);

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostAsync("/Admin/ResetTwoFactor", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["id"] = target.Id }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/Admin/Users")]
    [InlineData("/Admin/Index")]
    public async Task UserManagementPages_Render(string path)
    {
        // These pages threw a FormatException outside Arabic: IViewLocalizer runs
        // string.Format on write, so a resource value carrying a literal {0} placeholder -
        // which these pages rely on, passing it to JavaScript for client-side substitution -
        // blew up with no arguments supplied.
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UsersPage_ShowsTwoFactorStatusAndResetControl()
    {
        await CreateUserWithTwoFactorAsync("tfa_visible");

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var html = await client.GetStringAsync("/Admin/Users?pageSize=100");

        Assert.Contains("tfa_visible", html);
        // The reset control is rendered only for accounts that actually have a factor.
        Assert.Contains("resetTwoFactor(", html);
    }

    private async Task<string> CreateUserWithTwoFactorAsync(string userName)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = $"{userName}@example.test",
                EmailConfirmed = true,
                MustChangePassword = false
            };
            var created = await userManager.CreateAsync(user, SecurityWebApplicationFactory.TestPassword);
            Assert.True(created.Succeeded,
                string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        await userManager.ResetAuthenticatorKeyAsync(user);
        await userManager.SetTwoFactorEnabledAsync(user, true);

        return userName;
    }

    private async Task<(string Id, bool TwoFactorEnabled, string? AuthenticatorKey, string? SecurityStamp)>
        GetStateAsync(string userName)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByNameAsync(userName)
                   ?? throw new InvalidOperationException($"User {userName} not found.");

        return (
            user.Id,
            await userManager.GetTwoFactorEnabledAsync(user),
            await userManager.GetAuthenticatorKeyAsync(user),
            await userManager.GetSecurityStampAsync(user));
    }
}
