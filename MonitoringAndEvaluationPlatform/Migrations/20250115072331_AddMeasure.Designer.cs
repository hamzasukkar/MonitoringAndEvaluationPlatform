﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MonitoringAndEvaluationPlatform.Data;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250115072331_AddMeasure")]
    partial class AddMeasure
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Assessment", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Code");

                    b.ToTable("Assessment");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Donor", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<string>("Partner")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Code");

                    b.ToTable("Donor");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Framework", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<int>("IndicatorsPerformance")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("Trend")
                        .HasColumnType("float");

                    b.Property<double>("Weight")
                        .HasColumnType("float");

                    b.HasKey("Code");

                    b.ToTable("Framework");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Goal", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<int>("IndicatorsPerformance")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("Trend")
                        .HasColumnType("float");

                    b.HasKey("Code");

                    b.ToTable("Goal");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Indicator", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<bool>("Active")
                        .HasColumnType("bit");

                    b.Property<string>("Comment")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Concept")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("GAGRA")
                        .HasColumnType("float");

                    b.Property<double>("GAGRR")
                        .HasColumnType("float");

                    b.Property<int>("IndicatorsPerformance")
                        .HasColumnType("int");

                    b.Property<bool>("IsCommon")
                        .HasColumnType("bit");

                    b.Property<string>("MethodOfComputation")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SubOutputCode")
                        .HasColumnType("int");

                    b.Property<int>("Target")
                        .HasColumnType("int");

                    b.Property<DateTime>("TargetYear")
                        .HasColumnType("datetime2");

                    b.Property<double>("Trend")
                        .HasColumnType("float");

                    b.Property<double>("Weight")
                        .HasColumnType("float");

                    b.HasKey("Code");

                    b.HasIndex("SubOutputCode");

                    b.ToTable("Indicators");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Measure", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<int>("IndicatorCode")
                        .HasColumnType("int");

                    b.Property<double>("Value")
                        .HasColumnType("float");

                    b.Property<int>("ValueType")
                        .HasColumnType("int");

                    b.HasKey("Code");

                    b.HasIndex("IndicatorCode");

                    b.ToTable("Measure");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Ministrie", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<string>("Partner")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Code");

                    b.ToTable("Ministrie");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Outcome", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("FrameworkCode")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<int>("IndicatorsPerformance")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("Trend")
                        .HasColumnType("float");

                    b.Property<double>("Weight")
                        .HasColumnType("float");

                    b.HasKey("Code");

                    b.HasIndex("FrameworkCode");

                    b.ToTable("Outcomes");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Output", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<int>("IndicatorsPerformance")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OutcomeCode")
                        .HasColumnType("int");

                    b.Property<double>("Trend")
                        .HasColumnType("float");

                    b.Property<double>("Weight")
                        .HasColumnType("float");

                    b.HasKey("Code");

                    b.HasIndex("OutcomeCode");

                    b.ToTable("Outputs");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Program", b =>
                {
                    b.Property<int>("ProjectID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ProjectID"));

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<string>("Donor")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<double>("EstimatedBudget")
                        .HasColumnType("float");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<string>("ProjectManager")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProjectName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("RealBudget")
                        .HasColumnType("float");

                    b.Property<int>("RegionCode")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status1")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Status2")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SuperVisor")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Trend")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("performance")
                        .HasColumnType("int");

                    b.HasKey("ProjectID");

                    b.HasIndex("RegionCode");

                    b.ToTable("Program");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Region", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Code");

                    b.ToTable("Region");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Sector", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<string>("Partner")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Code");

                    b.ToTable("Sector");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.SubOutput", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<int>("IndicatorsPerformance")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OutputCode")
                        .HasColumnType("int");

                    b.Property<double>("Trend")
                        .HasColumnType("float");

                    b.Property<double>("Weight")
                        .HasColumnType("float");

                    b.HasKey("Code");

                    b.HasIndex("OutputCode");

                    b.ToTable("SubOutputs");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Target", b =>
                {
                    b.Property<int>("Code")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Code"));

                    b.Property<int>("DisbursementPerformance")
                        .HasColumnType("int");

                    b.Property<int>("FieldMonitoring")
                        .HasColumnType("int");

                    b.Property<int>("ImpactAssessment")
                        .HasColumnType("int");

                    b.Property<int>("IndicatorsPerformance")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("Trend")
                        .HasColumnType("float");

                    b.HasKey("Code");

                    b.ToTable("Target");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Indicator", b =>
                {
                    b.HasOne("MonitoringAndEvaluationPlatform.Models.SubOutput", "SubOutput")
                        .WithMany("Indicators")
                        .HasForeignKey("SubOutputCode")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SubOutput");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Measure", b =>
                {
                    b.HasOne("MonitoringAndEvaluationPlatform.Models.Indicator", "Indicator")
                        .WithMany("Measures")
                        .HasForeignKey("IndicatorCode")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Indicator");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Outcome", b =>
                {
                    b.HasOne("MonitoringAndEvaluationPlatform.Models.Framework", "Framework")
                        .WithMany("Outcomes")
                        .HasForeignKey("FrameworkCode")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Framework");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Output", b =>
                {
                    b.HasOne("MonitoringAndEvaluationPlatform.Models.Outcome", "Outcome")
                        .WithMany("Outputs")
                        .HasForeignKey("OutcomeCode")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Outcome");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Program", b =>
                {
                    b.HasOne("MonitoringAndEvaluationPlatform.Models.Region", "Region")
                        .WithMany()
                        .HasForeignKey("RegionCode")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Region");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.SubOutput", b =>
                {
                    b.HasOne("MonitoringAndEvaluationPlatform.Models.Output", "Output")
                        .WithMany("SubOutputs")
                        .HasForeignKey("OutputCode")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Output");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Framework", b =>
                {
                    b.Navigation("Outcomes");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Indicator", b =>
                {
                    b.Navigation("Measures");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Outcome", b =>
                {
                    b.Navigation("Outputs");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.Output", b =>
                {
                    b.Navigation("SubOutputs");
                });

            modelBuilder.Entity("MonitoringAndEvaluationPlatform.Models.SubOutput", b =>
                {
                    b.Navigation("Indicators");
                });
#pragma warning restore 612, 618
        }
    }
}
