using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// The audit log must never hold credentials: password hashes, security stamps, or the
/// authenticator keys and recovery codes Identity stores as IdentityUserToken.Value. Anyone who
/// can read AuditLogs (an administrator, a database backup) could otherwise attack the hashes
/// offline or generate valid two-factor codes.
///
/// SecurityWebApplicationFactory replaces the DbContext without interceptors, so these tests
/// build their own context with AuditInterceptor attached, exactly as Program.cs does.
/// </summary>
public class AuditRedactionTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"audit-redaction-{Guid.NewGuid():N}")
            .AddInterceptors(new AuditInterceptor(new HttpContextAccessor()))
            .Options);

    [Fact]
    public async Task PasswordHashAndSecurityStamp_AreNeverRecorded()
    {
        await using var context = CreateContext();

        var user = new ApplicationUser
        {
            UserName = "audit_probe",
            Email = "audit_probe@example.test",
            PasswordHash = "HASH-ONE",
            SecurityStamp = "STAMP-ONE"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.PasswordHash = "HASH-TWO";
        user.SecurityStamp = "STAMP-TWO";
        await context.SaveChangesAsync();

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        var logs = await context.AuditLogs.Where(a => a.EntityName == nameof(ApplicationUser)).ToListAsync();
        Assert.Equal(new[] { "Create", "Update", "Delete" }, logs.Select(l => l.Action).OrderBy(a => a == "Create" ? 0 : a == "Update" ? 1 : 2));

        var recorded = string.Join(" ", logs.Select(l => $"{l.OldValues} {l.NewValues}"));
        foreach (var secret in new[] { "HASH-ONE", "HASH-TWO", "STAMP-ONE", "STAMP-TWO" })
        {
            Assert.DoesNotContain(secret, recorded);
        }

        // The change itself stays visible, and ordinary fields are still audited.
        Assert.Contains(AuditInterceptor.Redacted, recorded);
        Assert.Contains("PasswordHash", logs.Single(l => l.Action == "Update").ChangedColumns);
        Assert.Contains("audit_probe@example.test", recorded);
    }

    [Fact]
    public async Task AuthenticatorKeyAndRecoveryCodes_AreNeverRecorded()
    {
        await using var context = CreateContext();

        var user = new ApplicationUser { UserName = "audit_2fa_probe", Email = "audit_2fa_probe@example.test" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var key = new IdentityUserToken<string>
        {
            UserId = user.Id, LoginProvider = "[AspNetUserStore]", Name = "AuthenticatorKey", Value = "TOTPSECRETONE"
        };
        var codes = new IdentityUserToken<string>
        {
            UserId = user.Id, LoginProvider = "[AspNetUserStore]", Name = "RecoveryCodes", Value = "CODE-AAA;CODE-BBB"
        };
        context.UserTokens.AddRange(key, codes);
        await context.SaveChangesAsync();

        key.Value = "TOTPSECRETTWO";
        await context.SaveChangesAsync();

        var logs = await context.AuditLogs.Where(a => a.EntityName.StartsWith("IdentityUserToken")).ToListAsync();
        Assert.Equal(3, logs.Count);

        var recorded = string.Join(" ", logs.Select(l => $"{l.OldValues} {l.NewValues}"));
        foreach (var secret in new[] { "TOTPSECRETONE", "TOTPSECRETTWO", "CODE-AAA", "CODE-BBB" })
        {
            Assert.DoesNotContain(secret, recorded);
        }

        // Which token changed is not secret and stays on record.
        Assert.Contains("AuthenticatorKey", recorded);
        Assert.Contains(AuditInterceptor.Redacted, recorded);
    }
}
