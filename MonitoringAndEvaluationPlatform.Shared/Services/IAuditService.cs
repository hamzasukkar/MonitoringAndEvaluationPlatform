using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IAuditService
    {
        Task LogAuthenticationAsync(string action, string? userId, string? userName, string? email, bool success, string? failureReason = null);
    }

    public class AuditService : IAuditService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public AuditService(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public async Task LogAuthenticationAsync(string action, string? userId, string? userName, string? email, bool success, string? failureReason = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

            var auditLog = new AuditLog
            {
                EntityName = "Authentication",
                EntityId = email ?? userName ?? "Unknown",
                Action = action,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent?.Length > 512 ? userAgent.Substring(0, 512) : userAgent,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Success = success,
                    Email = email,
                    FailureReason = failureReason
                })
            };

            // Use a new scope to avoid issues with DbContext lifetime
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync();
        }
    }
}
