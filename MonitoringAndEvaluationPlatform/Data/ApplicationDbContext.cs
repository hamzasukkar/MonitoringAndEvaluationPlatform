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
        public DbSet<MonitoringAndEvaluationPlatform.Models.Framework> Framework { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Outcome> Outcomes { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Output> Outputs { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Indicator> Indicators { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.SubOutput> SubOutputs { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Ministrie> Ministrie { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Program> Program { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Target> Target { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Goal> Goal { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Region> Region { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Sector> Sector { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Donor> Donor { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Assessment> Assessment { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Measure> Measure { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.LogicalFramework> LogicalFramework { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.LogicalFrameworkIndicator> LogicalFrameworkIndicator { get; set; } = default!;

    }
}
