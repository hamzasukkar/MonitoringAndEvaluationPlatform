using Microsoft.AspNetCore.Identity;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Infrastructure
{
    /// <summary>
    /// Creates the first System Administrator of a new installation.
    ///
    /// The operator puts the initial password in the server's own configuration:
    /// Bootstrap:AdminPassword in appsettings.json, which is not under source control, or the
    /// Bootstrap__AdminPassword environment variable. No password is ever taken from the
    /// repository. The setting is honoured only while no System Administrator exists, so it
    /// cannot be used to take over a running installation, and the account must choose its own
    /// password at first sign-in. The operator removes the setting once that has happened.
    /// </summary>
    public static class AdminBootstrap
    {
        public const string DefaultUserName = "admin";

        public enum Outcome
        {
            NotConfigured,
            Created,
            AdministratorExists,
            Failed
        }

        public static async Task<Outcome> EnsureFirstAdministratorAsync(
            UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
        {
            var password = configuration["Bootstrap:AdminPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                return Outcome.NotConfigured;
            }

            if ((await userManager.GetUsersInRoleAsync(UserRoles.SystemAdministrator)).Count > 0)
            {
                logger.LogWarning(
                    "Bootstrap:AdminPassword is set but a System Administrator already exists, so it is ignored. Remove it from the configuration.");
                return Outcome.AdministratorExists;
            }

            var userName = configuration["Bootstrap:AdminUserName"];
            var email = configuration["Bootstrap:AdminEmail"];
            var admin = new ApplicationUser
            {
                UserName = string.IsNullOrWhiteSpace(userName) ? DefaultUserName : userName.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                MustChangePassword = true
            };

            // CreateAsync applies the password policy, so a weak bootstrap password is refused.
            var created = await userManager.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                logger.LogError("Could not create the first System Administrator {UserName}: {Errors}",
                    admin.UserName, string.Join("; ", created.Errors.Select(e => e.Description)));
                return Outcome.Failed;
            }

            var granted = await userManager.AddToRoleAsync(admin, UserRoles.SystemAdministrator);
            if (!granted.Succeeded)
            {
                // An account without the role would only block the next attempt by name.
                await userManager.DeleteAsync(admin);
                logger.LogError("Could not grant the System Administrator role to {UserName}: {Errors}",
                    admin.UserName, string.Join("; ", granted.Errors.Select(e => e.Description)));
                return Outcome.Failed;
            }

            logger.LogWarning(
                "Created the first System Administrator {UserName}. It must choose a new password at first sign-in; then remove Bootstrap:AdminPassword from the configuration.",
                admin.UserName);
            return Outcome.Created;
        }
    }
}
