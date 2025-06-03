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
                .HasMany(p => p.Regions)
                .WithMany(r => r.Projects)
                .UsingEntity(j => j.ToTable("ProjectRegions")); // optional table name

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Sectors)
                .WithMany(r => r.Projects)
                .UsingEntity(j => j.ToTable("ProjectSectors")); // optional table name

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

            // Repeat for District, SubDistrict, and Community

            //// Seed Districts
            //modelBuilder.Entity<District>().HasData(
            //    new District { Code = 1, Name = "Saalihia", GovernorateCode = 1 },
            //    new District { Code = 2, Name = "Jablah", GovernorateCode = 2 },
            //    new District { Code = 3, Name = "Masyaf", GovernorateCode = 3 }
            //);

            //// Seed SubDistricts
            //modelBuilder.Entity<SubDistrict>().HasData(
            //    new SubDistrict { Code = 1, Name = "Muhairen", DistrictCode = 1 },
            //    new SubDistrict { Code = 2, Name = "Ein Elsharqiyeh", DistrictCode = 2 },
            //    new SubDistrict { Code = 3, Name = "Jeb Ramleh", DistrictCode = 3 }
            //);

            //// Seed Communities
            //modelBuilder.Entity<Community>().HasData(
            //    new Community { Code = 1, Name = "Alefaif", SubDistrictCode = 1 },
            //    new Community { Code = 2, Name = "Battara", SubDistrictCode = 2 },  
            //    new Community { Code = 3, Name = "Alamiyeh", SubDistrictCode = 3 }
            //);

        }
    }
}
