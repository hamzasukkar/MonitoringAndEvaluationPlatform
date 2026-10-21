using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// ReadingUser is a read-only role.
///
/// Measures, Plans, ActionPlans and ProjectPhases were governed by ministry scope alone, so a
/// ReadingUser of the owning ministry could record measurements and disbursement by posting
/// directly to those actions. Their write actions now require RecordProgress, which every
/// other role keeps.
/// </summary>
[Collection("security-app")]
public class ReadingUserWriteTests
{
    private const string ReadingUserName = "test_reading_a";

    private readonly SecurityWebApplicationFactory _factory;

    public ReadingUserWriteTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task ReadingUser_CannotRewriteOwnMinistrysPlan()
    {
        var plan = await CreatePlanAsync(_factory.MinistryACode);
        var client = await SignInReadingUserAsync();

        var response = await client.PostWithTokenAsync("/Plans/UpdatePlanValue",
            new Dictionary<string, string>
            {
                ["planCode"] = plan.Code.ToString(),
                ["valueType"] = "Realised",
                ["newValue"] = "424242"
            },
            // ReadingUser cannot open /Projects, the helper's default token page.
            tokenPage: "/Identity/Account/Manage/ChangePassword");

        AssertDenied(response);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.NotEqual(424242, (await context.Plans.FindAsync(plan.Code))!.Realised);
    }

    [Theory]
    [InlineData("/Measures/Create?phaseId={phase}")]
    [InlineData("/ProjectPhases/Edit/{phase}")]
    [InlineData("/Plans/Create")]
    [InlineData("/ActionPlans/Create")]
    public async Task ReadingUser_CannotOpenEntryForms(string pathTemplate)
    {
        var plan = await CreatePlanAsync(_factory.MinistryACode);
        var phaseId = await PhaseOfAsync(plan);
        var client = await SignInReadingUserAsync();

        var response = await client.GetAsync(pathTemplate.Replace("{phase}", phaseId.ToString()));

        AssertDenied(response);
    }

    [Fact]
    public async Task MinistriesUser_StillRecordsOwnMinistrysProgress()
    {
        // The new permission must not take recording away from the roles that keep it.
        var plan = await CreatePlanAsync(_factory.MinistryACode);

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.MinistryAUserName, SecurityWebApplicationFactory.TestPassword);

        var response = await client.PostWithTokenAsync("/Plans/UpdatePlanValue",
            new Dictionary<string, string>
            {
                ["planCode"] = plan.Code.ToString(),
                ["valueType"] = "Realised",
                ["newValue"] = "7"
            });

        // Authorization passed: the action answered with its own JSON, not a redirect.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\"", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> SignInReadingUserAsync()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            if (await userManager.FindByNameAsync(ReadingUserName) is null)
            {
                var user = new ApplicationUser
                {
                    UserName = ReadingUserName,
                    Email = $"{ReadingUserName}@example.test",
                    EmailConfirmed = true,
                    MinistryCode = _factory.MinistryACode,
                    MustChangePassword = false
                };
                var created = await userManager.CreateAsync(user, SecurityWebApplicationFactory.TestPassword);
                Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
                await userManager.AddToRoleAsync(user, UserRoles.ReadingUser);
            }
        }

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(ReadingUserName, SecurityWebApplicationFactory.TestPassword);
        return client;
    }

    /// <summary>A policy failure under cookie authentication is a redirect to AccessDenied.</summary>
    private static void AssertDenied(HttpResponseMessage response)
    {
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found)
        {
            Assert.Contains("AccessDenied", response.Headers.Location?.ToString() ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<int> PhaseOfAsync(Plan plan)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await context.ActionPlans.FindAsync(plan.ActionPlanCode))!.ProjectPhaseId;
    }

    private async Task<Plan> CreatePlanAsync(int ministryCode)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var project = new Project
        {
            ProjectName = $"Reading-user host {Guid.NewGuid():N}",
            MinistryCode = ministryCode,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            EstimatedBudget = 1000
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

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
}
