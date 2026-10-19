using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Account-level protections: failed-sign-in lockout, the forced first-sign-in password
/// change, and refusing to delete a ministry that records still point at.
/// </summary>
[Collection("security-app")]
public class AccountProtectionTests
{
    private readonly SecurityWebApplicationFactory _factory;

    public AccountProtectionTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task FiveWrongPasswords_LockTheAccount()
    {
        var userName = await CreateUserAsync(mustChangePassword: false);
        var client = _factory.CreateNonRedirectingClient();

        for (var i = 0; i < 5; i++)
        {
            await client.LoginAsync(userName, "Wrong!Password#2026");
        }

        // Even the right password is refused while the lockout lasts. The login page is shown
        // again with the (Arabic) locked-account message rather than redirecting.
        var response = await client.LoginAsync(userName, SecurityWebApplicationFactory.TestPassword);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("هذا الحساب مقفل", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));

        var protectedPage = await client.GetAsync("/");
        Assert.Contains("/Identity/Account/Login", protectedPage.Headers.Location?.ToString() ?? string.Empty);

        using var scope = _factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByNameAsync(userName);
        Assert.True(user!.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task AccountFlaggedMustChangePassword_IsSentToChangePassword()
    {
        var userName = await CreateUserAsync(mustChangePassword: true);
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(userName, SecurityWebApplicationFactory.TestPassword);

        // "/" rather than a permission-gated page: for a page the role cannot open,
        // authorization answers (AccessDenied) before the password-change check runs.
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Manage/ChangePassword", response.Headers.Location?.ToString() ?? string.Empty);

        // The change-password page itself must stay reachable, or the user is stuck.
        var page = await client.GetAsync("/Identity/Account/Manage/ChangePassword");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
    }

    [Fact]
    public async Task MinistryWithLinkedRecords_IsNotDeleted()
    {
        // Ministry A has a user. Its project and output links would cascade away silently,
        // so the delete must be refused before it reaches the database.
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Ministries/InlineDelete",
            new Dictionary<string, string> { ["id"] = _factory.MinistryACode.ToString() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await context.Ministries.AnyAsync(m => m.Code == _factory.MinistryACode));
    }

    [Fact]
    public async Task MinistryWithNothingLinked_CanBeDeleted()
    {
        // The check must not block every delete.
        int code;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ministry = new Ministry
            {
                MinistryDisplayName_AR = "وزارة بلا ارتباطات",
                MinistryDisplayName_EN = "Unlinked ministry",
                MinistryUserName = $"Unlinked{Guid.NewGuid():N}"[..20],
                Logo = "🏛"
            };
            context.Ministries.Add(ministry);
            await context.SaveChangesAsync();
            code = ministry.Code;
        }

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Ministries/InlineDelete",
            new Dictionary<string, string> { ["id"] = code.ToString() });

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        using var verify = _factory.Services.CreateScope();
        Assert.False(await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Ministries.AnyAsync(m => m.Code == code));
    }

    private async Task<string> CreateUserAsync(bool mustChangePassword)
    {
        var userName = $"acct_{Guid.NewGuid():N}"[..20];

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await userManager.CreateAsync(new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            EmailConfirmed = true,
            MustChangePassword = mustChangePassword
        }, SecurityWebApplicationFactory.TestPassword);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync((await userManager.FindByNameAsync(userName))!, UserRoles.ReadingUser);
        return userName;
    }
}
