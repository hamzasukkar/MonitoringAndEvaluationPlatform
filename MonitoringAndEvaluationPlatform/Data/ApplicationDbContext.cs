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
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<SubDistrict> SubDistricts { get; set; }
        public DbSet<Community> Communities { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<Target> Targets { get; set; }
        public DbSet<SDGIndicator> sDGIndicators { get; set; }
        public DbSet<UNorganization> UNorganizations { get; set; }
        public DbSet<DonorCountry> DonorCountries { get; set; }



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

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Sectors)
                .WithMany(r => r.Projects)
                .UsingEntity(j => j.ToTable("ProjectSectors")); // optional table name

            modelBuilder.Entity<Project>()
            .HasMany(p => p.Donors)
            .WithMany(r => r.Projects)
            .UsingEntity(j => j.ToTable("ProjectDonors")); // optional table name

            modelBuilder.Entity<Project>()
            .HasMany(p => p.Ministries)
            .WithMany(r => r.Projects)
            .UsingEntity(j => j.ToTable("ProjectMinistries")); // optional table name

            // Project <-> Governorate
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Governorates)
                .WithMany(g => g.projects)
                .UsingEntity(j => j.ToTable("ProjectGovernorates"));

            // Project <-> District
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Districts)
                .WithMany(d => d.projects)
                .UsingEntity(j => j.ToTable("ProjectDistricts"));

            // Project <-> SubDistrict
            modelBuilder.Entity<Project>()
                .HasMany(p => p.SubDistricts)
                .WithMany(s => s.projects)
                .UsingEntity(j => j.ToTable("ProjectSubDistricts"));

            // Project <-> Community
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Communities)
                .WithMany(c => c.projects)
                .UsingEntity(j => j.ToTable("ProjectCommunities"));






        }
    }
}
