using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Boots the real application pipeline against an in-memory database.
///
/// The point is to exercise the actual middleware order, authorization policies and
/// antiforgery configuration, not a stand-in - a security regression that only shows up
/// once the whole pipeline is assembled is exactly the kind this suite must catch.
/// </summary>
public class SecurityWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"security-tests-{Guid.NewGuid():N}";

    public const string AdminUserName = "test_admin";
    public const string MinistryAUserName = "test_ministry_a";
    public const string MinistryBUserName = "test_ministry_b";
    public const string ReaderUserName = "test_reader";
    public const string TestPassword = "Test!Password#2026";

    public int MinistryACode { get; private set; }
    public int MinistryBCode { get; private set; }

    public SecurityWebApplicationFactory()
    {
        // Program.cs reads the connection string during WebApplication.CreateBuilder and
        // throws if it is blank. That happens before ConfigureAppConfiguration callbacks are
        // applied, so the value has to come from the environment. The real provider is
        // swapped for the in-memory one in ConfigureWebHost, so this only needs to be
        // non-blank.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=(test);Database=test;Trusted_Connection=True;");
        Environment.SetEnvironmentVariable("Seeding__EnableDemoUsers", "false");

        // The suite signs in many times from one address; the production limit of 10 per
        // five minutes would throttle the tests themselves. RateLimitingTests overrides this
        // back down to assert the limiter actually works.
        Environment.SetEnvironmentVariable("RateLimiting__Login__PermitLimit", "10000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production so the Development-only branches (developer exception page, demo
        // seeding) stay off, and so cookie/HSTS behaviour matches a real deployment.
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            // Replace SQL Server with an in-memory provider.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }

    /// <summary>
    /// Creates the fixture: one administrator, two ministries with one user each, one
    /// role-less reader, and a project per ministry. Cross-ministry tests rely on
    /// ministry A and ministry B being genuinely separate.
    /// </summary>
    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        var context = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[]
                 {
                     UserRoles.SystemAdministrator, UserRoles.MinistriesUser,
                     UserRoles.DataEntry, UserRoles.ReadingUser, UserRoles.MinistryStrategyManager
                 })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var ministryA = new Ministry
        {
            MinistryDisplayName_AR = "وزارة الاختبار أ",
            MinistryDisplayName_EN = "Test Ministry A",
            MinistryUserName = "TestA",
            Logo = "🏛"
        };
        var ministryB = new Ministry
        {
            MinistryDisplayName_AR = "وزارة الاختبار ب",
            MinistryDisplayName_EN = "Test Ministry B",
            MinistryUserName = "TestB",
            Logo = "🏛"
        };
        context.Ministries.AddRange(ministryA, ministryB);
        await context.SaveChangesAsync();

        MinistryACode = ministryA.Code;
        MinistryBCode = ministryB.Code;

        await CreateUserAsync(userManager, AdminUserName, UserRoles.SystemAdministrator, null);
        await CreateUserAsync(userManager, MinistryAUserName, UserRoles.MinistriesUser, ministryA.Code);
        await CreateUserAsync(userManager, MinistryBUserName, UserRoles.MinistriesUser, ministryB.Code);
        // Deliberately role-less and ministry-less: represents a bare authenticated principal.
        await CreateUserAsync(userManager, ReaderUserName, null, null);
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager, string userName, string? role, int? ministryCode)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            EmailConfirmed = true,
            MinistryCode = ministryCode,
            // Explicitly false: these fixtures must not be bounced to the change-password page.
            MustChangePassword = false
        };

        var result = await userManager.CreateAsync(user, TestPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create test user {userName}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        if (role != null)
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    /// <summary>
    /// An HttpClient that does not follow redirects, so 302s stay observable.
    ///
    /// The base address is https deliberately. Outside Development the auth cookie is issued
    /// with CookieSecurePolicy.Always, so an http test client would silently never send it
    /// and every authenticated request would bounce back to the login page.
    /// </summary>
    public HttpClient CreateNonRedirectingClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
}
