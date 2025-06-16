using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class Models : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MinistryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donors",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Partner = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donors", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Frameworks",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Frameworks", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Governorates",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governorates", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Ministries",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinistryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ministries", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "project2s",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project2s", x => x.ProjectID);
                });

            migrationBuilder.CreateTable(
                name: "ProjectManagers",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectManagers", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SuperVisors",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperVisors", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outcomes",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    FrameworkCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outcomes", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Outcomes_Frameworks_FrameworkCode",
                        column: x => x.FrameworkCode,
                        principalTable: "Frameworks",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GovernorateCode = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Districts_Governorates_GovernorateCode",
                        column: x => x.GovernorateCode,
                        principalTable: "Governorates",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project2Governorates",
                columns: table => new
                {
                    GovernoratesCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    project2sProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project2Governorates", x => new { x.GovernoratesCode, x.project2sProjectID });
                    table.ForeignKey(
                        name: "FK_Project2Governorates_Governorates_GovernoratesCode",
                        column: x => x.GovernoratesCode,
                        principalTable: "Governorates",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project2Governorates_project2s_project2sProjectID",
                        column: x => x.project2sProjectID,
                        principalTable: "project2s",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedBudget = table.Column<double>(type: "float", nullable: false),
                    RealBudget = table.Column<double>(type: "float", nullable: false),
                    ProjectManagerCode = table.Column<int>(type: "int", nullable: false),
                    SuperVisorCode = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    performance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false),
                    Financial = table.Column<int>(type: "int", nullable: false),
                    Physical = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectID);
                    table.ForeignKey(
                        name: "FK_Projects_ProjectManagers_ProjectManagerCode",
                        column: x => x.ProjectManagerCode,
                        principalTable: "ProjectManagers",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Projects_SuperVisors_SuperVisorCode",
                        column: x => x.SuperVisorCode,
                        principalTable: "SuperVisors",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outputs",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    OutcomeCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outputs", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Outputs_Outcomes_OutcomeCode",
                        column: x => x.OutcomeCode,
                        principalTable: "Outcomes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project2Districts",
                columns: table => new
                {
                    DistrictsCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    project2sProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project2Districts", x => new { x.DistrictsCode, x.project2sProjectID });
                    table.ForeignKey(
                        name: "FK_Project2Districts_Districts_DistrictsCode",
                        column: x => x.DistrictsCode,
                        principalTable: "Districts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project2Districts_project2s_project2sProjectID",
                        column: x => x.project2sProjectID,
                        principalTable: "project2s",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubDistricts",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistrictCode = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubDistricts", x => x.Code);
                    table.ForeignKey(
                        name: "FK_SubDistricts_Districts_DistrictCode",
                        column: x => x.DistrictCode,
                        principalTable: "Districts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionPlans",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlansCount = table.Column<int>(type: "int", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionPlans", x => x.Code);
                    table.ForeignKey(
                        name: "FK_ActionPlans_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logicalFrameworks",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Performance = table.Column<double>(type: "float", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logicalFrameworks", x => x.Code);
                    table.ForeignKey(
                        name: "FK_logicalFrameworks_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDistricts",
                columns: table => new
                {
                    DistrictsCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    projectsProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDistricts", x => new { x.DistrictsCode, x.projectsProjectID });
                    table.ForeignKey(
                        name: "FK_ProjectDistricts_Districts_DistrictsCode",
                        column: x => x.DistrictsCode,
                        principalTable: "Districts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDistricts_Projects_projectsProjectID",
                        column: x => x.projectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDonors",
                columns: table => new
                {
                    DonorsCode = table.Column<int>(type: "int", nullable: false),
                    ProjectsProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDonors", x => new { x.DonorsCode, x.ProjectsProjectID });
                    table.ForeignKey(
                        name: "FK_ProjectDonors_Donors_DonorsCode",
                        column: x => x.DonorsCode,
                        principalTable: "Donors",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDonors_Projects_ProjectsProjectID",
                        column: x => x.ProjectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectGovernorates",
                columns: table => new
                {
                    GovernoratesCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    projectsProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectGovernorates", x => new { x.GovernoratesCode, x.projectsProjectID });
                    table.ForeignKey(
                        name: "FK_ProjectGovernorates_Governorates_GovernoratesCode",
                        column: x => x.GovernoratesCode,
                        principalTable: "Governorates",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectGovernorates_Projects_projectsProjectID",
                        column: x => x.projectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMinistries",
                columns: table => new
                {
                    MinistriesCode = table.Column<int>(type: "int", nullable: false),
                    ProjectsProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMinistries", x => new { x.MinistriesCode, x.ProjectsProjectID });
                    table.ForeignKey(
                        name: "FK_ProjectMinistries_Ministries_MinistriesCode",
                        column: x => x.MinistriesCode,
                        principalTable: "Ministries",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMinistries_Projects_ProjectsProjectID",
                        column: x => x.ProjectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSectors",
                columns: table => new
                {
                    ProjectsProjectID = table.Column<int>(type: "int", nullable: false),
                    SectorsCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSectors", x => new { x.ProjectsProjectID, x.SectorsCode });
                    table.ForeignKey(
                        name: "FK_ProjectSectors_Projects_ProjectsProjectID",
                        column: x => x.ProjectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectSectors_Sectors_SectorsCode",
                        column: x => x.SectorsCode,
                        principalTable: "Sectors",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubOutputs",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    OutputCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubOutputs", x => x.Code);
                    table.ForeignKey(
                        name: "FK_SubOutputs_Outputs_OutputCode",
                        column: x => x.OutputCode,
                        principalTable: "Outputs",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Communities",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubDistrictCode = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Communities_SubDistricts_SubDistrictCode",
                        column: x => x.SubDistrictCode,
                        principalTable: "SubDistricts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project2SubDistricts",
                columns: table => new
                {
                    SubDistrictsCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    project2sProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project2SubDistricts", x => new { x.SubDistrictsCode, x.project2sProjectID });
                    table.ForeignKey(
                        name: "FK_Project2SubDistricts_SubDistricts_SubDistrictsCode",
                        column: x => x.SubDistrictsCode,
                        principalTable: "SubDistricts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project2SubDistricts_project2s_project2sProjectID",
                        column: x => x.project2sProjectID,
                        principalTable: "project2s",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSubDistricts",
                columns: table => new
                {
                    SubDistrictsCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    projectsProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSubDistricts", x => new { x.SubDistrictsCode, x.projectsProjectID });
                    table.ForeignKey(
                        name: "FK_ProjectSubDistricts_Projects_projectsProjectID",
                        column: x => x.projectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectSubDistricts_SubDistricts_SubDistrictsCode",
                        column: x => x.SubDistrictsCode,
                        principalTable: "SubDistricts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    ActionPlanCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Activities_ActionPlans_ActionPlanCode",
                        column: x => x.ActionPlanCode,
                        principalTable: "ActionPlans",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logicalFrameworkIndicators",
                columns: table => new
                {
                    IndicatorCode = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Performance = table.Column<double>(type: "float", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    LogicalFrameworkCode = table.Column<int>(type: "int", nullable: false),
                    IsCommon = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Target = table.Column<int>(type: "int", nullable: false),
                    TargetYear = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GAGRA = table.Column<double>(type: "float", nullable: false),
                    GAGRR = table.Column<double>(type: "float", nullable: false),
                    Concept = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logicalFrameworkIndicators", x => x.IndicatorCode);
                    table.ForeignKey(
                        name: "FK_logicalFrameworkIndicators_logicalFrameworks_LogicalFrameworkCode",
                        column: x => x.LogicalFrameworkCode,
                        principalTable: "logicalFrameworks",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Indicators",
                columns: table => new
                {
                    IndicatorCode = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    SubOutputCode = table.Column<int>(type: "int", nullable: false),
                    IsCommon = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Target = table.Column<int>(type: "int", nullable: false),
                    TargetYear = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GAGRA = table.Column<double>(type: "float", nullable: false),
                    GAGRR = table.Column<double>(type: "float", nullable: false),
                    Concept = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MethodOfComputation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicators", x => x.IndicatorCode);
                    table.ForeignKey(
                        name: "FK_Indicators_SubOutputs_SubOutputCode",
                        column: x => x.SubOutputCode,
                        principalTable: "SubOutputs",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project2Communities",
                columns: table => new
                {
                    CommunitiesCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    project2sProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project2Communities", x => new { x.CommunitiesCode, x.project2sProjectID });
                    table.ForeignKey(
                        name: "FK_Project2Communities_Communities_CommunitiesCode",
                        column: x => x.CommunitiesCode,
                        principalTable: "Communities",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project2Communities_project2s_project2sProjectID",
                        column: x => x.project2sProjectID,
                        principalTable: "project2s",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCommunities",
                columns: table => new
                {
                    CommunitiesCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    projectsProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCommunities", x => new { x.CommunitiesCode, x.projectsProjectID });
                    table.ForeignKey(
                        name: "FK_ProjectCommunities_Communities_CommunitiesCode",
                        column: x => x.CommunitiesCode,
                        principalTable: "Communities",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectCommunities_Projects_projectsProjectID",
                        column: x => x.projectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Planned = table.Column<int>(type: "int", nullable: false),
                    Realised = table.Column<int>(type: "int", nullable: false),
                    ActivityCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Plans_Activities_ActivityCode",
                        column: x => x.ActivityCode,
                        principalTable: "Activities",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logicalMeasures",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    LogicalFrameworkIndicatorIndicatorCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logicalMeasures", x => x.Code);
                    table.ForeignKey(
                        name: "FK_logicalMeasures_logicalFrameworkIndicators_LogicalFrameworkIndicatorIndicatorCode",
                        column: x => x.LogicalFrameworkIndicatorIndicatorCode,
                        principalTable: "logicalFrameworkIndicators",
                        principalColumn: "IndicatorCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Measures",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    IndicatorCode = table.Column<int>(type: "int", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measures", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Measures_Indicators_IndicatorCode",
                        column: x => x.IndicatorCode,
                        principalTable: "Indicators",
                        principalColumn: "IndicatorCode",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Measures_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectIndicators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    IndicatorCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectIndicators_Indicators_IndicatorCode",
                        column: x => x.IndicatorCode,
                        principalTable: "Indicators",
                        principalColumn: "IndicatorCode",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectIndicators_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionPlans_ProjectID",
                table: "ActionPlans",
                column: "ProjectID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActionPlanCode",
                table: "Activities",
                column: "ActionPlanCode");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Communities_SubDistrictCode",
                table: "Communities",
                column: "SubDistrictCode");

            migrationBuilder.CreateIndex(
                name: "IX_Districts_GovernorateCode",
                table: "Districts",
                column: "GovernorateCode");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_SubOutputCode",
                table: "Indicators",
                column: "SubOutputCode");

            migrationBuilder.CreateIndex(
                name: "IX_logicalFrameworkIndicators_LogicalFrameworkCode",
                table: "logicalFrameworkIndicators",
                column: "LogicalFrameworkCode");

            migrationBuilder.CreateIndex(
                name: "IX_logicalFrameworks_ProjectID",
                table: "logicalFrameworks",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_logicalMeasures_LogicalFrameworkIndicatorIndicatorCode",
                table: "logicalMeasures",
                column: "LogicalFrameworkIndicatorIndicatorCode");

            migrationBuilder.CreateIndex(
                name: "IX_Measures_IndicatorCode",
                table: "Measures",
                column: "IndicatorCode");

            migrationBuilder.CreateIndex(
                name: "IX_Measures_ProjectID",
                table: "Measures",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Outcomes_FrameworkCode",
                table: "Outcomes",
                column: "FrameworkCode");

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_OutcomeCode",
                table: "Outputs",
                column: "OutcomeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_ActivityCode",
                table: "Plans",
                column: "ActivityCode");

            migrationBuilder.CreateIndex(
                name: "IX_Project2Communities_project2sProjectID",
                table: "Project2Communities",
                column: "project2sProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Project2Districts_project2sProjectID",
                table: "Project2Districts",
                column: "project2sProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Project2Governorates_project2sProjectID",
                table: "Project2Governorates",
                column: "project2sProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Project2SubDistricts_project2sProjectID",
                table: "Project2SubDistricts",
                column: "project2sProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCommunities_projectsProjectID",
                table: "ProjectCommunities",
                column: "projectsProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDistricts_projectsProjectID",
                table: "ProjectDistricts",
                column: "projectsProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDonors_ProjectsProjectID",
                table: "ProjectDonors",
                column: "ProjectsProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId",
                table: "ProjectFiles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGovernorates_projectsProjectID",
                table: "ProjectGovernorates",
                column: "projectsProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIndicators_IndicatorCode",
                table: "ProjectIndicators",
                column: "IndicatorCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIndicators_ProjectId",
                table: "ProjectIndicators",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMinistries_ProjectsProjectID",
                table: "ProjectMinistries",
                column: "ProjectsProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectManagerCode",
                table: "Projects",
                column: "ProjectManagerCode");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SuperVisorCode",
                table: "Projects",
                column: "SuperVisorCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSectors_SectorsCode",
                table: "ProjectSectors",
                column: "SectorsCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSubDistricts_projectsProjectID",
                table: "ProjectSubDistricts",
                column: "projectsProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_SubDistricts_DistrictCode",
                table: "SubDistricts",
                column: "DistrictCode");

            migrationBuilder.CreateIndex(
                name: "IX_SubOutputs_OutputCode",
                table: "SubOutputs",
                column: "OutputCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "logicalMeasures");

            migrationBuilder.DropTable(
                name: "Measures");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "Project2Communities");

            migrationBuilder.DropTable(
                name: "Project2Districts");

            migrationBuilder.DropTable(
                name: "Project2Governorates");

            migrationBuilder.DropTable(
                name: "Project2SubDistricts");

            migrationBuilder.DropTable(
                name: "ProjectCommunities");

            migrationBuilder.DropTable(
                name: "ProjectDistricts");

            migrationBuilder.DropTable(
                name: "ProjectDonors");

            migrationBuilder.DropTable(
                name: "ProjectFiles");

            migrationBuilder.DropTable(
                name: "ProjectGovernorates");

            migrationBuilder.DropTable(
                name: "ProjectIndicators");

            migrationBuilder.DropTable(
                name: "ProjectMinistries");

            migrationBuilder.DropTable(
                name: "ProjectSectors");

            migrationBuilder.DropTable(
                name: "ProjectSubDistricts");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "logicalFrameworkIndicators");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "project2s");

            migrationBuilder.DropTable(
                name: "Communities");

            migrationBuilder.DropTable(
                name: "Donors");

            migrationBuilder.DropTable(
                name: "Indicators");

            migrationBuilder.DropTable(
                name: "Ministries");

            migrationBuilder.DropTable(
                name: "Sectors");

            migrationBuilder.DropTable(
                name: "logicalFrameworks");

            migrationBuilder.DropTable(
                name: "ActionPlans");

            migrationBuilder.DropTable(
                name: "SubDistricts");

            migrationBuilder.DropTable(
                name: "SubOutputs");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.DropTable(
                name: "Outputs");

            migrationBuilder.DropTable(
                name: "ProjectManagers");

            migrationBuilder.DropTable(
                name: "SuperVisors");

            migrationBuilder.DropTable(
                name: "Governorates");

            migrationBuilder.DropTable(
                name: "Outcomes");

            migrationBuilder.DropTable(
                name: "Frameworks");
        }
    }
}
