using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Cross-ministry access control.
///
/// Plans, Measures, ProjectPhases and ActionPlans used to be loaded by ID with no ownership
/// check at all, so any authenticated user could read - and rewrite - another ministry's
/// disbursement figures and performance evidence by incrementing an ID.
/// </summary>
[Collection("security-app")]
public class MinistryScopeTests
{
    private readonly SecurityWebApplicationFactory _factory;

    public MinistryScopeTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Scope_IsFailClosed_ForUserWithoutMinistry()
    {
        // A user with no MinistryCode must be scoped to nothing, never to everything.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var accessor = new StubHttpContextAccessor(_factory, SecurityWebApplicationFactory.ReaderUserName);
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();

        var service = new MinistryScopeService(context, userManager, accessor);

        var (isAdmin, ministryCode) = await service.GetScopeAsync();
        Assert.False(isAdmin);
        Assert.Null(ministryCode);

        // Every ownership check must deny.
        Assert.False(await service.ProjectBelongsToScopeAsync(1));
        Assert.False(await service.PlanBelongsToScopeAsync(1));
        Assert.False(await service.MeasureBelongsToScopeAsync(1));
        Assert.False(await service.ProjectPhaseBelongsToScopeAsync(1));
        Assert.False(await service.ActionPlanBelongsToScopeAsync(1));
        Assert.False(await service.SubOutputBelongsToScopeAsync(1));

        // And the query filters must return nothing rather than everything.
        var projects = await service.ApplyProjectScopeAsync(context.Projects);
        Assert.Empty(projects);

        var frameworks = await service.ApplyFrameworkScopeAsync(context.Frameworks);
        Assert.Empty(frameworks);
    }

    [Fact]
    public async Task MinistryUser_CannotTouchAnotherMinistrysProject()
    {
        var projectB = await CreateProjectAsync(_factory.MinistryBCode, "Ministry B project");

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.MinistryAUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.GetAsync($"/Projects/Details/{projectB.ProjectID}");

        await AssertDeniedAsync(response, mustNotContain: "Ministry B project");
    }

    [Fact]
    public async Task MinistryUser_CannotRewriteAnotherMinistrysPlan()
    {
        // The most severe of the IDOR findings: Plans.FindAsync(planCode) followed by a write
        // to Realised, with no scope check, on a controller with no [Authorize] at all.
        var planB = await CreatePlanAsync(_factory.MinistryBCode);

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.MinistryAUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Plans/UpdatePlanValue",
            new Dictionary<string, string>
            {
                ["planCode"] = planB.Code.ToString(),
                ["valueType"] = "Realised",
                ["newValue"] = "999999"
            });

        await AssertDeniedAsync(response);

        // And the value must be unchanged on disk.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var reloaded = await context.Plans.FindAsync(planB.Code);
        Assert.NotEqual(999999, reloaded!.Realised);
    }

    [Fact]
    public async Task MinistryUser_CannotReadAnotherMinistrysPhaseMeasures()
    {
        var phaseB = await CreatePhaseAsync(_factory.MinistryBCode);

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.MinistryAUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.GetAsync($"/Measures/GetMeasuresByPhase?phaseId={phaseB.Id}");

        await AssertDeniedAsync(response);
    }

    [Fact]
    public async Task MinistryUser_CannotEditAnotherMinistrysPhase()
    {
        var phaseB = await CreatePhaseAsync(_factory.MinistryBCode);

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(SecurityWebApplicationFactory.MinistryAUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.GetAsync($"/ProjectPhases/Edit/{phaseB.Id}");

        await AssertDeniedAsync(response);
    }

    [Fact]
    public async Task Administrator_IsNotBlockedByMinistryScope()
    {
        // The scope checks must not lock administrators out of other ministries' data.
        // Asserted against the scope service rather than a rendered page: the fixture's
        // minimal Project rows do not satisfy everything the Details view loads, and that
        // is a fixture limitation, not an authorization outcome.
        var projectB = await CreateProjectAsync(_factory.MinistryBCode, "Admin-visible project");
        var phaseB = await CreatePhaseAsync(_factory.MinistryBCode);
        var planB = await CreatePlanAsync(_factory.MinistryBCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();

        var service = new MinistryScopeService(
            context, userManager,
            new StubHttpContextAccessor(_factory, SecurityWebApplicationFactory.AdminUserName));

        var (isAdmin, _) = await service.GetScopeAsync();
        Assert.True(isAdmin);

        Assert.True(await service.ProjectBelongsToScopeAsync(projectB.ProjectID));
        Assert.True(await service.ProjectPhaseBelongsToScopeAsync(phaseB.Id));
        Assert.True(await service.PlanBelongsToScopeAsync(planB.Code));

        // Admin queries are unfiltered.
        var projects = await service.ApplyProjectScopeAsync(context.Projects);
        Assert.Contains(projects, p => p.ProjectID == projectB.ProjectID);
    }

    [Fact]
    public async Task MinistryUser_QueryScope_ExcludesOtherMinistries()
    {
        var projectA = await CreateProjectAsync(_factory.MinistryACode, "Ministry A project");
        var projectB = await CreateProjectAsync(_factory.MinistryBCode, "Other ministry project");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();

        var service = new MinistryScopeService(
            context, userManager,
            new StubHttpContextAccessor(_factory, SecurityWebApplicationFactory.MinistryAUserName));

        var projects = (await service.ApplyProjectScopeAsync(context.Projects)).ToList();

        Assert.Contains(projects, p => p.ProjectID == projectA.ProjectID);
        Assert.DoesNotContain(projects, p => p.ProjectID == projectB.ProjectID);
    }

    /// <summary>
    /// Asserts a request was denied. With cookie authentication a controller's Forbid()
    /// becomes a 302 to AccessDenied rather than a bare 403, and some actions answer a
    /// non-owned ID with 404. All are acceptable; serving the record is not.
    /// </summary>
    private static async Task AssertDeniedAsync(HttpResponseMessage response, string? mustNotContain = null)
    {
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);

        if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found)
        {
            var location = response.Headers.Location?.ToString() ?? string.Empty;
            Assert.True(
                location.Contains("AccessDenied", StringComparison.OrdinalIgnoreCase)
                || location.Contains("Login", StringComparison.OrdinalIgnoreCase),
                $"Denied requests must redirect to AccessDenied or Login, not {location}.");
        }

        if (mustNotContain != null)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(mustNotContain, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<Project> CreateProjectAsync(int ministryCode, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var project = new Project
        {
            ProjectName = name,
            MinistryCode = ministryCode,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            EstimatedBudget = 1000
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private async Task<ProjectPhase> CreatePhaseAsync(int ministryCode)
    {
        var project = await CreateProjectAsync(ministryCode, $"Phase host {Guid.NewGuid():N}");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var phase = new ProjectPhase
        {
            Name = "Phase 1",
            ProjectID = project.ProjectID,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Budget = 100,
            Weight = 100
        };
        context.ProjectPhases.Add(phase);
        await context.SaveChangesAsync();
        return phase;
    }

    private async Task<Plan> CreatePlanAsync(int ministryCode)
    {
        var phase = await CreatePhaseAsync(ministryCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var actionPlan = new ActionPlan { ProjectPhaseId = phase.Id, PlansCount = 1 };
        context.ActionPlans.Add(actionPlan);
        await context.SaveChangesAsync();

        var plan = new Plan
        {
            ActionPlanCode = actionPlan.Code,
            Name = "Month 1",
            Date = new DateTime(2026, 1, 31),
            Realised = 1
        };
        context.Plans.Add(plan);
        await context.SaveChangesAsync();
        return plan;
    }

    /// <summary>
    /// Supplies a ClaimsPrincipal for the named user so the scope service can be exercised
    /// directly, without going through HTTP.
    /// </summary>
    private sealed class StubHttpContextAccessor : IHttpContextAccessor
    {
        public StubHttpContextAccessor(SecurityWebApplicationFactory factory, string userName)
        {
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var signInManager = scope.ServiceProvider
                .GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser>>();

            var user = userManager.FindByNameAsync(userName).GetAwaiter().GetResult()
                       ?? throw new InvalidOperationException($"Test user {userName} not found.");
            var principal = signInManager.CreateUserPrincipalAsync(user).GetAwaiter().GetResult();

            HttpContext = new DefaultHttpContext { User = principal };
        }

        public HttpContext? HttpContext { get; set; }
    }
}
