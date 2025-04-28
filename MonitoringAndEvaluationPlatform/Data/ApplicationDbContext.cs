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
        public DbSet<Framework> Frameworks { get; set; } = default!;
        public DbSet<Outcome> Outcomes { get; set; } = default!;
        public DbSet<Output> Outputs { get; set; } = default!;
        public DbSet<Indicator> Indicators { get; set; } = default!;
        public DbSet<SubOutput> SubOutputs { get; set; } = default!;
        public DbSet<Ministry> Ministries { get; set; } = default!;
        public DbSet<Project> Projects { get; set; } = default!;
        public DbSet<Region> Regions { get; set; } = default!;
        public DbSet<Sector> Sectors { get; set; } = default!;
        public DbSet<Donor> Donors { get; set; } = default!;
        public DbSet<Measure> Measures { get; set; } = default!;
        public DbSet<LogicalMeasure> logicalMeasures { get; set; } = default!;
        public DbSet<SuperVisor> SuperVisors { get; set; } = default!;
        public DbSet<ProjectManager> ProjectManagers { get; set; } = default!;
        public DbSet<Activity> Activities { get; set; } = default!;
        public DbSet<Plan> Plans { get; set; } = default!;
        public DbSet<ActionPlan> ActionPlans { get; set; } = default!;
        public DbSet<ProjectIndicator> ProjectIndicators { get; set; } = default!;
        public DbSet<LogicalFramework> logicalFrameworks { get; set; } = default!;
        public DbSet<LogicalFrameworkIndicator> logicalFrameworkIndicators { get; set; } = default!;
        public DbSet<ProjectFile> ProjectFiles { get; set; }

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

            modelBuilder.Entity<ProjectIndicator>()
                .HasOne(pi => pi.Project)
                .WithMany(p => p.ProjectIndicators)
                .HasForeignKey(pi => pi.ProjectId);

            modelBuilder.Entity<ProjectIndicator>()
                .HasOne(pi => pi.Indicator)
                .WithMany(i => i.ProjectIndicators)
                .HasForeignKey(pi => pi.IndicatorCode);

        }
    }
}
