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
        public DbSet<Models.Project> Project { get; set; } = default!;
        public DbSet<Target> Target { get; set; } = default!;
        public DbSet<Goal> Goal { get; set; } = default!;
        public DbSet<Region> Regions { get; set; } = default!;
        public DbSet<Sector> Sectors { get; set; } = default!;
        public DbSet<Donor> Donors { get; set; } = default!;
        public DbSet<Assessment> Assessment { get; set; } = default!;
        public DbSet<Measure> Measures { get; set; } = default!;
        public DbSet<LogicalFramework> LogicalFramework { get; set; } = default!;
        public DbSet<LogicalFrameworkIndicator> LogicalFrameworkIndicator { get; set; } = default!;
        public DbSet<SuperVisor> SuperVisors { get; set; } = default!;
        public DbSet<ProjectManager> ProjectManagers { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure Code is used as the primary key
            modelBuilder.Entity<Measure>()
                .HasKey(m => m.Code);

            modelBuilder.Entity<Measure>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Measures)
                .HasForeignKey(m => m.ProjectID);

            modelBuilder.Entity<Measure>()
                .HasOne(m => m.Indicator)
                .WithMany(i => i.Measures)
                .HasForeignKey(m => m.IndicatorCode);

            modelBuilder.Entity<Project>()
               .HasOne(p => p.ActionPlan)
               .WithOne(ap => ap.Project)
               .HasForeignKey<ActionPlan>(ap => ap.ProjectID)
               .OnDelete(DeleteBehavior.Cascade); // Ensures deletion propagates

        }
        public DbSet<MonitoringAndEvaluationPlatform.Models.Activity> Activity { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.Plan> Plan { get; set; } = default!;
        public DbSet<MonitoringAndEvaluationPlatform.Models.ActionPlan> ActionPlan { get; set; } = default!;
    }
}
