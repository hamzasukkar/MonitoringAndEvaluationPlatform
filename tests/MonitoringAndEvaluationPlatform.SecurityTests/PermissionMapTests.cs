using System.Reflection;
using MonitoringAndEvaluationPlatform.Infrastructure;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Guards the role -> permission map.
///
/// The enforcement logic (PermissionAuthorizationHandler) and the display logic
/// (RolePermissionService, which renders /Admin/Roles) used to hold separate copies of this
/// switch, and they had already drifted: the handler granted MinistryStrategyManager broad
/// strategy and project permissions that the admin screen did not show. An operator auditing
/// permissions through the UI was being told something weaker than what was enforced.
/// </summary>
public class PermissionMapTests
{
    public static TheoryData<string, string> AllRolePermissionPairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var role in AllRoles())
        {
            foreach (var permission in AllPermissions())
            {
                data.Add(role, permission);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllRolePermissionPairs))]
    public void DisplayedPermissions_MatchEnforcedPermissions(string role, string permission)
    {
        var enforced = PermissionMap.HasPermission(new[] { role }, permission);
        var displayed = RolePermissionService.GetPermissionsForRole(role).Contains(permission);

        Assert.True(
            enforced == displayed,
            $"/Admin/Roles and the authorization handler disagree for role '{role}' and " +
            $"permission '{permission}': enforced={enforced}, displayed={displayed}.");
    }

    [Fact]
    public void SystemAdministrator_HasEveryPermission()
    {
        foreach (var permission in AllPermissions())
        {
            Assert.True(
                PermissionMap.HasPermission(new[] { UserRoles.SystemAdministrator }, permission),
                $"SystemAdministrator should hold {permission}.");
        }
    }

    [Fact]
    public void UnknownPermission_IsDenied()
    {
        // The switch must fail closed: an unmapped permission denies rather than grants.
        foreach (var role in AllRoles().Where(r => r != UserRoles.SystemAdministrator))
        {
            Assert.False(PermissionMap.HasPermission(new[] { role }, "Permissions.DoesNotExist"));
        }
    }

    [Fact]
    public void NoRoles_IsDeniedEverythingExceptLoginAndPasswordRecovery()
    {
        // A self-registered, role-less account must not inherit anything by default.
        foreach (var permission in AllPermissions())
        {
            var granted = PermissionMap.HasPermission(Array.Empty<string>(), permission);

            if (permission == Permissions.Login || permission == Permissions.RecoverPassword)
            {
                continue;
            }

            Assert.False(granted, $"A role-less principal must not hold {permission}.");
        }
    }

    [Fact]
    public void EveryEnforcedPermission_AppearsInTheAdminScreenCategories()
    {
        // GetAllPermissions() drives the /Admin/Roles matrix. A permission that is enforced
        // but missing from the categories dictionary is invisible to an auditing operator.
        var displayed = RolePermissionService.GetAllPermissions().ToHashSet();

        foreach (var permission in AllPermissions())
        {
            Assert.True(displayed.Contains(permission),
                $"{permission} is enforced but missing from RolePermissionService categories.");
        }
    }

    private static IEnumerable<string> AllPermissions() =>
        typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .Distinct();

    private static IEnumerable<string> AllRoles() =>
        typeof(UserRoles)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .Distinct();
}
