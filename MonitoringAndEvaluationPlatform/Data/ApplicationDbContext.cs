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
        public DbSet<Project2> project2s { get; set; } = default!;
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

            modelBuilder.Entity<Project>()
            .HasOne(p => p.Governorate)
            .WithMany()
            .HasForeignKey(p => p.GovernorateCode)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
            .HasOne(p => p.District)
            .WithMany()
            .HasForeignKey(p => p.DistrictCode)
            .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Project>()
           .HasOne(p => p.SubDistrict)
           .WithMany()
           .HasForeignKey(p => p.SubDistrictCode)
           .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
              .HasOne(p => p.Community)
              .WithMany()
              .HasForeignKey(p => p.CommunityCode)
              .OnDelete(DeleteBehavior.Restrict);

            // Project <-> Governorate
            modelBuilder.Entity<Project2>()
                .HasMany(p => p.Governorates)
                .WithMany(g => g.project2s)
                .UsingEntity(j => j.ToTable("ProjectGovernorates"));

            // Project <-> District
            modelBuilder.Entity<Project2>()
                .HasMany(p => p.Districts)
                .WithMany(d => d.project2s)
                .UsingEntity(j => j.ToTable("ProjectDistricts"));

            // Project <-> SubDistrict
            modelBuilder.Entity<Project2>()
                .HasMany(p => p.SubDistricts)
                .WithMany(s => s.project2s)
                .UsingEntity(j => j.ToTable("ProjectSubDistricts"));

            // Project <-> Community
            modelBuilder.Entity<Project2>()
                .HasMany(p => p.Communities)
                .WithMany(c => c.project2s)
                .UsingEntity(j => j.ToTable("ProjectCommunities"));

            // Repeat for District, SubDistrict, and Community

            // Sample Governorate
            modelBuilder.Entity<Governorate>().HasData(
                new Governorate { Code = "GOV001", Name = "Governorate A" },
                new Governorate { Code = "GOV002", Name = "Governorate B" }
            );

            // Sample Districts
            modelBuilder.Entity<District>().HasData(
                new District { Code = "D001", Name = "District 1", GovernorateCode = "GOV001" },
                new District { Code = "D002", Name = "District 2", GovernorateCode = "GOV001" },
                new District { Code = "D003", Name = "District 3", GovernorateCode = "GOV002" }
            );

            // Sample SubDistricts
            modelBuilder.Entity<SubDistrict>().HasData(
                new SubDistrict { Code = "SD001", Name = "SubDistrict 1", DistrictCode = "D001" },
                new SubDistrict { Code = "SD002", Name = "SubDistrict 2", DistrictCode = "D002" }
            );

            // Sample Communities
            modelBuilder.Entity<Community>().HasData(
                new Community { Code = "C001", Name = "Community 1", SubDistrictCode = "SD001" },
                new Community { Code = "C002", Name = "Community 2", SubDistrictCode = "SD002" }
            );

        }
    }
}
