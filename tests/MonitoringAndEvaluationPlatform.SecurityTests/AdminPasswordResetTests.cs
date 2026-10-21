using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Administrative password reset.
///
/// After a reset the administrator knows the account's password, so the account holder must
/// be made to replace it at the next sign-in - otherwise a password someone else chose stays
/// in use indefinitely.
/// </summary>
[Collection("security-app")]
public class AdminPasswordResetTests
{
    private const string NewPassword = "Reset-Pass#2026x";

    private readonly SecurityWebApplicationFactory _factory;

    public AdminPasswordResetTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Reset_FlagsAccountToChooseANewPassword()
    {
        var id = await CreateUserAsync("pw_reset_target");

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Admin/ResetPassword",
            new Dictionary<string, string> { ["id"] = id, ["newPassword"] = NewPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\":true", await response.Content.ReadAsStringAsync());

        var (mustChange, passwordWorks) = await GetStateAsync(id, NewPassword);
        Assert.True(passwordWorks);
        Assert.True(mustChange);
    }

    [Fact]
    public async Task RejectedReset_LeavesAccountUnchanged()
    {
        var id = await CreateUserAsync("pw_reset_weak");

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        // Fails the password policy, so nothing about the account may change.
        var response = await client.PostWithTokenAsync("/Admin/ResetPassword",
            new Dictionary<string, string> { ["id"] = id, ["newPassword"] = "short" });

        Assert.Contains("\"success\":false", await response.Content.ReadAsStringAsync());

        var (mustChange, oldPasswordWorks) = await GetStateAsync(id, SecurityWebApplicationFactory.TestPassword);
        Assert.True(oldPasswordWorks);
        Assert.False(mustChange);
    }

    private async Task<string> CreateUserAsync(string userName)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            EmailConfirmed = true,
            MustChangePassword = false
        };
        var created = await userManager.CreateAsync(user, SecurityWebApplicationFactory.TestPassword);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        return user.Id;
    }

    private async Task<(bool MustChangePassword, bool PasswordWorks)> GetStateAsync(string id, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByIdAsync(id)
                   ?? throw new InvalidOperationException($"User {id} not found.");

        return (user.MustChangePassword, await userManager.CheckPasswordAsync(user, password));
    }
}
