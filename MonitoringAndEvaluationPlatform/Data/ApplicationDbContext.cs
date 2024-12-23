using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<MonitoringAndEvaluationPlatform.Models.Freamework> Freamework { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Outcome> Outcomes { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Output> Outputs { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Indicator> Indicators { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.SubOutput> SubOutputs { get; set; } = default!;
    }
}
