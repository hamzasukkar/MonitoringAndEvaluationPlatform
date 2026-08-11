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
        public DbSet<ProjectPhase> ProjectPhases { get; set; } = default!;
        public DbSet<Sector> Sectors { get; set; } = default!;
        public DbSet<PublicSectorType> PublicSectorTypes { get; set; } = default!;
        public DbSet<Donor> Donors { get; set; } = default!;
        public DbSet<Measure> Measures { get; set; } = default!;
        public DbSet<MeasureFile> MeasureFiles { get; set; } = default!;
        public DbSet<SuperVisor> SuperVisors { get; set; } = default!;
        public DbSet<ProjectManager> ProjectManagers { get; set; } = default!;
        public DbSet<Plan> Plans { get; set; } = default!;
        public DbSet<ActionPlan> ActionPlans { get; set; } = default!;
        public DbSet<ProjectDonor> ProjectDonors { get; set; } = default!;
        public DbSet<ProjectFile> ProjectFiles { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<SubDistrict> SubDistricts { get; set; }
        public DbSet<Community> Communities { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<Target> Targets { get; set; }
        public DbSet<SDGIndicator> sDGIndicators { get; set; }
        public DbSet<FrameworkGoal> FrameworkGoals { get; set; }
        public DbSet<FrameworkGoalYearlyValue> FrameworkGoalYearlyValues { get; set; }
        public DbSet<FrameworkGoalFile> FrameworkGoalFiles { get; set; }
        public DbSet<FrameworkGoalManualExpectedTarget> FrameworkGoalManualExpectedTargets { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<GuideSection> GuideSections { get; set; } = default!;
        public DbSet<GuideSectionVersion> GuideSectionVersions { get; set; } = default!;
        public DbSet<Request> Requests { get; set; } = default!;
        public DbSet<RequestFile> RequestFiles { get; set; } = default!;
        public DbSet<PlatformVersion> PlatformVersions { get; set; } = default!;
        public DbSet<RequestEmployee> RequestEmployees { get; set; } = default!;
        public DbSet<RequestComment> RequestComments { get; set; } = default!;
        public DbSet<RequestCommentAttachment> RequestCommentAttachments { get; set; } = default!;
        public DbSet<RequestTest> RequestTests { get; set; } = default!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Request number is the business key surfaced to users; keep it unique so a
            // concurrent insert fails loudly instead of duplicating a number.
            modelBuilder.Entity<Request>()
                .HasIndex(r => r.RequestNumber)
                .IsUnique();

            // Restrict on both user links: cascading from ApplicationUser through two
            // paths would create multiple cascade paths on SQL Server.
            modelBuilder.Entity<Request>()
                .HasOne(r => r.SubmittedByUser)
                .WithMany()
                .HasForeignKey(r => r.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Request>()
                .HasOne(r => r.AssignedToUser)
                .WithMany()
                .HasForeignKey(r => r.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Request>()
                .HasOne(r => r.Ministry)
                .WithMany()
                .HasForeignKey(r => r.MinistryCode)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RequestFile>()
                .HasOne(f => f.Request)
                .WithMany(r => r.Files)
                .HasForeignKey(f => f.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Platform versions: unique version number; deleting a version detaches
            // its requests rather than deleting them.
            modelBuilder.Entity<PlatformVersion>()
                .HasIndex(v => v.VersionNumber)
                .IsUnique();

            modelBuilder.Entity<Request>()
                .HasOne(r => r.PlatformVersion)
                .WithMany(v => v.Requests)
                .HasForeignKey(r => r.PlatformVersionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Deleting an employee detaches their requests rather than deleting them.
            modelBuilder.Entity<Request>()
                .HasOne(r => r.RequestEmployee)
                .WithMany(e => e.Requests)
                .HasForeignKey(r => r.RequestEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Comments die with their request.
            modelBuilder.Entity<RequestComment>()
                .HasOne(c => c.Request)
                .WithMany(r => r.Comments)
                .HasForeignKey(c => c.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Self-reference for replies: NoAction avoids a second cascade path on
            // SQL Server (the Request cascade already removes the whole thread).
            modelBuilder.Entity<RequestComment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RequestComment>()
                .HasOne(c => c.AuthorUser)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RequestComment>()
                .HasOne(c => c.OnBehalfOfEmployee)
                .WithMany()
                .HasForeignKey(c => c.OnBehalfOfEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RequestCommentAttachment>()
                .HasOne(a => a.RequestComment)
                .WithMany(c => c.Attachments)
                .HasForeignKey(a => a.RequestCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Test sign-offs die with their request. This is the only cascading path
            // into RequestTests, so SQL Server sees no multiple cascade paths.
            modelBuilder.Entity<RequestTest>()
                .HasOne(t => t.Request)
                .WithMany(r => r.Tests)
                .HasForeignKey(t => t.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // The named employee is the identity of the tick, so the FK is required and
            // SetNull is impossible. Restrict keeps the verification record intact;
            // RequestEmployeesController.Delete blocks the delete with a clear message
            // instead of silently erasing who signed off on what.
            modelBuilder.Entity<RequestTest>()
                .HasOne(t => t.RequestEmployee)
                .WithMany(e => e.Tests)
                .HasForeignKey(t => t.RequestEmployeeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RequestTest>()
                .HasOne(t => t.RecordedByUser)
                .WithMany()
                .HasForeignKey(t => t.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One tick per employee per request — the real guard against a double submit.
            modelBuilder.Entity<RequestTest>()
                .HasIndex(t => new { t.RequestId, t.RequestEmployeeId })
                .IsUnique();

            // Measure primary key
            modelBuilder.Entity<Measure>()
                .HasKey(m => m.Code);

            // Measure → ProjectPhase (replaces old Measure → Indicator)
            modelBuilder.Entity<Measure>()
                .HasOne(m => m.ProjectPhase)
                .WithMany(pp => pp.Measures)
                .HasForeignKey(m => m.ProjectPhaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProjectPhase → Project (one-to-many)
            modelBuilder.Entity<ProjectPhase>()
                .HasOne(pp => pp.Project)
                .WithMany(p => p.Phases)
                .HasForeignKey(pp => pp.ProjectID)
                .OnDelete(DeleteBehavior.Cascade);

            // ProjectPhase.Weight decimal precision
            modelBuilder.Entity<ProjectPhase>()
                .Property(pp => pp.Weight)
                .HasPrecision(5, 2);

            // ActionPlan → ProjectPhase (one-to-one, replaces old ActionPlan → Project)
            modelBuilder.Entity<ActionPlan>()
                .HasOne(ap => ap.ProjectPhase)
                .WithOne(pp => pp.ActionPlan)
                .HasForeignKey<ActionPlan>(ap => ap.ProjectPhaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Plan → ActionPlan (many-to-one)
            modelBuilder.Entity<Plan>()
                .HasOne(p => p.ActionPlan)
                .WithMany(ap => ap.Plans)
                .HasForeignKey(p => p.ActionPlanCode)
                .OnDelete(DeleteBehavior.Cascade);

            // Indicator → Project (many-to-one, replaces old ProjectIndicator many-to-many)
            modelBuilder.Entity<Indicator>()
                .HasOne(i => i.Project)
                .WithMany(p => p.Indicators)
                .HasForeignKey(i => i.ProjectID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Project>()
                .HasMany(p => p.Sectors)
                .WithMany(r => r.Projects)
                .UsingEntity(j => j.ToTable("ProjectSectors"));

            // Project → PublicSectorType (many-to-one, only set when the Public sector is selected)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.PublicSectorType)
                .WithMany(t => t.Projects)
                .HasForeignKey(p => p.PublicSectorTypeCode)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure explicit ProjectDonor relationship
            modelBuilder.Entity<ProjectDonor>()
                .HasOne(pd => pd.Project)
                .WithMany(p => p.ProjectDonors)
                .HasForeignKey(pd => pd.ProjectId);

            modelBuilder.Entity<ProjectDonor>()
                .HasOne(pd => pd.Donor)
                .WithMany(d => d.ProjectDonors)
                .HasForeignKey(pd => pd.DonorCode);

            // Configure decimal precision for ProjectDonor
            modelBuilder.Entity<ProjectDonor>()
                .Property(pd => pd.FundingPercentage)
                .HasPrecision(5, 2);

            modelBuilder.Entity<ProjectDonor>()
                .Property(pd => pd.FundingAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Project>()
            .HasMany(p => p.Ministries)
            .WithMany(r => r.Projects)
            .UsingEntity(j => j.ToTable("ProjectMinistries"));

            // Configure single Ministry relationship
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Ministry)
                .WithMany()
                .HasForeignKey(p => p.MinistryCode)
                .IsRequired(false);

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

            // Framework <-> FrameworkGoal (one-to-many)
            modelBuilder.Entity<FrameworkGoal>()
                .HasOne(fg => fg.Framework)
                .WithMany(f => f.Goals)
                .HasForeignKey(fg => fg.FrameworkCode)
                .OnDelete(DeleteBehavior.Cascade);

            // FrameworkGoal <-> FrameworkGoalYearlyValue (one-to-many)
            modelBuilder.Entity<FrameworkGoalYearlyValue>()
                .HasOne(fgv => fgv.FrameworkGoal)
                .WithMany(fg => fg.YearlyValues)
                .HasForeignKey(fgv => fgv.FrameworkGoalID)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: One value per year per goal
            modelBuilder.Entity<FrameworkGoalYearlyValue>()
                .HasIndex(fgv => new { fgv.FrameworkGoalID, fgv.Year })
                .IsUnique();

            // FrameworkGoal <-> FrameworkGoalFile (one-to-many)
            modelBuilder.Entity<FrameworkGoalFile>()
                .HasOne(fgf => fgf.FrameworkGoal)
                .WithMany(fg => fg.Attachments)
                .HasForeignKey(fgf => fgf.FrameworkGoalID)
                .OnDelete(DeleteBehavior.Cascade);

            // FrameworkGoal <-> FrameworkGoalManualExpectedTarget (one-to-many)
            modelBuilder.Entity<FrameworkGoalManualExpectedTarget>()
                .HasOne(m => m.FrameworkGoal)
                .WithMany(fg => fg.ManualExpectedTargets)
                .HasForeignKey(m => m.FrameworkGoalID)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one manual expected target per year per goal
            modelBuilder.Entity<FrameworkGoalManualExpectedTarget>()
                .HasIndex(m => new { m.FrameworkGoalID, m.Year })
                .IsUnique();

            // MeasureFile → Measure (one-to-many)
            modelBuilder.Entity<MeasureFile>()
                .HasOne(mf => mf.Measure)
                .WithMany(m => m.Files)
                .HasForeignKey(mf => mf.MeasureCode)
                .OnDelete(DeleteBehavior.Cascade);

            // AuditLog indexes for better query performance
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.EntityName);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityName, a.EntityId });

            // One override row per guide section.
            modelBuilder.Entity<GuideSection>()
                .HasIndex(g => g.SectionKey)
                .IsUnique();

            modelBuilder.Entity<GuideSectionVersion>()
                .HasOne(v => v.GuideSection)
                .WithMany(s => s.Versions)
                .HasForeignKey(v => v.GuideSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
