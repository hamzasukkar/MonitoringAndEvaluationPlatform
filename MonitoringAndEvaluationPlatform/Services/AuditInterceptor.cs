using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MonitoringAndEvaluationPlatform.Models;
using System.Text.Json;

namespace MonitoringAndEvaluationPlatform.Services
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        /// <summary>Stands in for a credential in OldValues/NewValues.</summary>
        public const string Redacted = "[redacted]";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

            var auditLogs = new List<AuditLog>();
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = httpContext?.User?.Identity?.Name;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

            foreach (var entry in eventData.Context.ChangeTracker.Entries())
            {
                // Skip AuditLog entity to avoid infinite loop
                if (entry.Entity is AuditLog)
                    continue;

                // Only track entities we care about
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditLog = new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent?.Length > 512 ? userAgent.Substring(0, 512) : userAgent
                };

                // Get primary key value
                var primaryKey = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                auditLog.EntityId = primaryKey?.CurrentValue?.ToString() ?? "Unknown";

                auditLog.EntityDisplayName = GetEntityDisplayName(entry);

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditLog.Action = "Create";
                        auditLog.NewValues = SerializeProperties(entry.Properties
                            .Where(p => p.CurrentValue != null)
                            .ToDictionary(p => p.Metadata.Name, p => AuditValue(entry, p, p.CurrentValue)));
                        break;

                    case EntityState.Modified:
                        auditLog.Action = "Update";
                        var modifiedProperties = entry.Properties
                            .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                            .ToList();

                        if (!modifiedProperties.Any())
                            continue;

                        auditLog.OldValues = SerializeProperties(modifiedProperties
                            .ToDictionary(p => p.Metadata.Name, p => AuditValue(entry, p, p.OriginalValue)));
                        auditLog.NewValues = SerializeProperties(modifiedProperties
                            .ToDictionary(p => p.Metadata.Name, p => AuditValue(entry, p, p.CurrentValue)));
                        auditLog.ChangedColumns = string.Join(", ", modifiedProperties
                            .Select(p => p.Metadata.Name));
                        break;

                    case EntityState.Deleted:
                        auditLog.Action = "Delete";
                        auditLog.OldValues = SerializeProperties(entry.Properties
                            .Where(p => p.OriginalValue != null)
                            .ToDictionary(p => p.Metadata.Name, p => AuditValue(entry, p, p.OriginalValue)));
                        break;

                    default:
                        continue;
                }

                auditLogs.Add(auditLog);
            }

            if (auditLogs.Any())
            {
                eventData.Context.Set<AuditLog>().AddRange(auditLogs);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// The value to record for one property. Credentials are replaced with
        /// <see cref="Redacted"/>: the column name still lands in ChangedColumns, so the log shows
        /// that a password or second factor changed, but never the hash, the security stamp, or
        /// an authenticator key / recovery codes (stored as IdentityUserToken.Value). Recording
        /// those would let anyone who can read the audit log - an administrator, a database
        /// backup - generate valid sign-in codes or attack the hashes offline.
        /// </summary>
        private static object? AuditValue(EntityEntry entry, PropertyEntry property, object? value)
        {
            if (value is null)
                return null;

            var name = property.Metadata.Name;
            var isCredential = entry.Entity switch
            {
                IdentityUser => name is nameof(IdentityUser.PasswordHash) or nameof(IdentityUser.SecurityStamp),
                IdentityUserToken<string> => name == nameof(IdentityUserToken<string>.Value),
                _ => false
            };

            return isCredential ? Redacted : value;
        }

        private static string? GetEntityDisplayName(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var typeName = entry.Entity.GetType().Name;
            var candidateNames = new[] { $"{typeName}Name", "Name", "Title", "UserName", "MinistryDisplayName" };

            foreach (var candidate in candidateNames)
            {
                var property = entry.Properties.FirstOrDefault(p =>
                    string.Equals(p.Metadata.Name, candidate, StringComparison.OrdinalIgnoreCase));

                var value = (property?.CurrentValue ?? property?.OriginalValue)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Length > 256 ? value.Substring(0, 256) : value;
            }

            return null;
        }

        private string? SerializeProperties(Dictionary<string, object?> properties)
        {
            if (properties == null || !properties.Any())
                return null;

            try
            {
                return JsonSerializer.Serialize(properties, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    MaxDepth = 2
                });
            }
            catch
            {
                // If serialization fails, return a simple representation
                return string.Join("; ", properties.Select(p => $"{p.Key}={p.Value}"));
            }
        }
    }
}
