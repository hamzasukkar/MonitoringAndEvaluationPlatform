using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MonitoringAndEvaluationPlatform.Models;
using System.Text.Json;

namespace MonitoringAndEvaluationPlatform.Services
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
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

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditLog.Action = "Create";
                        auditLog.NewValues = SerializeProperties(entry.Properties
                            .Where(p => p.CurrentValue != null)
                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                        break;

                    case EntityState.Modified:
                        auditLog.Action = "Update";
                        var modifiedProperties = entry.Properties
                            .Where(p => p.IsModified)
                            .ToList();

                        auditLog.OldValues = SerializeProperties(modifiedProperties
                            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                        auditLog.NewValues = SerializeProperties(modifiedProperties
                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                        auditLog.ChangedColumns = string.Join(", ", modifiedProperties
                            .Select(p => p.Metadata.Name));
                        break;

                    case EntityState.Deleted:
                        auditLog.Action = "Delete";
                        auditLog.OldValues = SerializeProperties(entry.Properties
                            .Where(p => p.OriginalValue != null)
                            .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
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
