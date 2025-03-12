using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Framework> Framework { get; set; } = default!;
        public DbSet<Outcome> Outcomes { get; set; } = default!;
        public DbSet<Output> Outputs { get; set; } = default!;
        public DbSet<Indicator> Indicators { get; set; } = default!;
        public DbSet<SubOutput> SubOutputs { get; set; } = default!;
        public DbSet<Ministry> Ministry { get; set; } = default!;
        public DbSet<Models.Program> Program { get; set; } = default!;
        public DbSet<Target> Target { get; set; } = default!;
        public DbSet<Goal> Goal { get; set; } = default!;
        public DbSet<Region> Region { get; set; } = default!;
        public DbSet<Sector> Sector { get; set; } = default!;
        public DbSet<Donor> Donor { get; set; } = default!;
        public DbSet<Assessment> Assessment { get; set; } = default!;
        public DbSet<Measure> Measure { get; set; } = default!;
        public DbSet<LogicalFramework> LogicalFramework { get; set; } = default!;
        public DbSet<LogicalFrameworkIndicator> LogicalFrameworkIndicator { get; set; } = default!;
        public DbSet<SuperVisor> SuperVisor { get; set; } = default!;
        public DbSet<ProjectManager> ProjectManager { get; set; } = default!;

    }
}
