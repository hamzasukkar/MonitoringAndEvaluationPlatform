using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MonitoringAndEvaluationPlatform.Infrastructure;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// First administrator of a new installation (Bootstrap:AdminPassword).
///
/// A new database has no account at all and, outside Development, no demo account is seeded,
/// so the operator's setting is the way in. It must create one administrator who has to replace
/// the password, and must do nothing once an administrator exists - otherwise anyone who learned
/// the setting could add an administrator to a running installation.
///
/// Each test boots its own host: the bootstrap runs at startup against a fresh database.
/// </summary>
public class AdminBootstrapTests
{
    private const string BootstrapPassword = "First-Admin#2026x";

    [Fact]
    public async Task NewInstallation_CreatesAdministratorWhoMustChangePassword()
    {
        // Configuration is read when the host is built, so the setting goes in the environment.
        Environment.SetEnvironmentVariable("Bootstrap__AdminPassword", BootstrapPassword);
        Environment.SetEnvironmentVariable("Bootstrap__AdminUserName", "first_admin");
        try
        {
            using var factory = new SecurityWebApplicationFactory();
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var admin = await userManager.FindByNameAsync("first_admin");
            Assert.NotNull(admin);
            Assert.True(admin!.MustChangePassword);
            Assert.True(await userManager.IsInRoleAsync(admin, UserRoles.SystemAdministrator));
            Assert.True(await userManager.CheckPasswordAsync(admin, BootstrapPassword));
        }
        finally
        {
            Environment.SetEnvironmentVariable("Bootstrap__AdminPassword", null);
            Environment.SetEnvironmentVariable("Bootstrap__AdminUserName", null);
        }
    }

    [Fact]
    public async Task ExistingAdministrator_SettingIsIgnored()
    {
        using var factory = new SecurityWebApplicationFactory();
        await factory.SeedAsync(); // includes a System Administrator
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var outcome = await AdminBootstrap.EnsureFirstAdministratorAsync(
            userManager, Settings(BootstrapPassword, "intruder_admin"), NullLogger.Instance);

        Assert.Equal(AdminBootstrap.Outcome.AdministratorExists, outcome);
        Assert.Null(await userManager.FindByNameAsync("intruder_admin"));
        Assert.Single(await userManager.GetUsersInRoleAsync(UserRoles.SystemAdministrator));
    }

    [Fact]
    public async Task WithoutSetting_NoAccountIsCreated_AndAWeakPasswordIsRefused()
    {
        using var factory = new SecurityWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Startup ran without the setting: no account of any kind exists.
        Assert.Empty(userManager.Users);

        var outcome = await AdminBootstrap.EnsureFirstAdministratorAsync(
            userManager, Settings("short", null), NullLogger.Instance);

        Assert.Equal(AdminBootstrap.Outcome.Failed, outcome);
        Assert.Empty(userManager.Users);
    }

    private static IConfiguration Settings(string password, string? userName) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bootstrap:AdminPassword"] = password,
                ["Bootstrap:AdminUserName"] = userName
            })
            .Build();
}
