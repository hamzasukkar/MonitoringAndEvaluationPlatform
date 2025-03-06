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
                    MinistrieName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "Assessment",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessment", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Donor",
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
                    table.PrimaryKey("PK_Donor", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Framework",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Framework", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Goal",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goal", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Ministrie",
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
                    table.PrimaryKey("PK_Ministrie", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ProjectManager",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectManager", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Region",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Region", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Sector",
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
                    table.PrimaryKey("PK_Sector", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SuperVisor",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperVisor", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Target",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Target", x => x.Code);
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
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    FrameworkCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outcomes", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Outcomes_Framework_FrameworkCode",
                        column: x => x.FrameworkCode,
                        principalTable: "Framework",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Program",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegionCode = table.Column<int>(type: "int", nullable: false),
                    EstimatedBudget = table.Column<double>(type: "float", nullable: false),
                    RealBudget = table.Column<double>(type: "float", nullable: false),
                    Trend = table.Column<int>(type: "int", nullable: false),
                    ProjectManagerCode = table.Column<int>(type: "int", nullable: false),
                    SuperVisorCode = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinistrieCode = table.Column<int>(type: "int", nullable: false),
                    DonorCode = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    performance = table.Column<int>(type: "int", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Program", x => x.ProjectID);
                    table.ForeignKey(
                        name: "FK_Program_Donor_DonorCode",
                        column: x => x.DonorCode,
                        principalTable: "Donor",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Program_Ministrie_MinistrieCode",
                        column: x => x.MinistrieCode,
                        principalTable: "Ministrie",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Program_ProjectManager_ProjectManagerCode",
                        column: x => x.ProjectManagerCode,
                        principalTable: "ProjectManager",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Program_Region_RegionCode",
                        column: x => x.RegionCode,
                        principalTable: "Region",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Program_SuperVisor_SuperVisorCode",
                        column: x => x.SuperVisorCode,
                        principalTable: "SuperVisor",
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
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false),
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
                name: "LogicalFramework",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    ProgramProjectID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogicalFramework", x => x.Code);
                    table.ForeignKey(
                        name: "FK_LogicalFramework_Program_ProgramProjectID",
                        column: x => x.ProgramProjectID,
                        principalTable: "Program",
                        principalColumn: "ProjectID");
                });

            migrationBuilder.CreateTable(
                name: "SubOutputs",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false),
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
                name: "LogicalFrameworkIndicator",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    LogicalFrameworkCode = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_LogicalFrameworkIndicator", x => x.Code);
                    table.ForeignKey(
                        name: "FK_LogicalFrameworkIndicator_LogicalFramework_LogicalFrameworkCode",
                        column: x => x.LogicalFrameworkCode,
                        principalTable: "LogicalFramework",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Indicators",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trend = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Indicators", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Indicators_SubOutputs_SubOutputCode",
                        column: x => x.SubOutputCode,
                        principalTable: "SubOutputs",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Measure",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    IndicatorCode = table.Column<int>(type: "int", nullable: false),
                    LogicalFrameworkIndicatorCode = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measure", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Measure_Indicators_IndicatorCode",
                        column: x => x.IndicatorCode,
                        principalTable: "Indicators",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Measure_LogicalFrameworkIndicator_LogicalFrameworkIndicatorCode",
                        column: x => x.LogicalFrameworkIndicatorCode,
                        principalTable: "LogicalFrameworkIndicator",
                        principalColumn: "Code");
                });

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
                name: "IX_Indicators_SubOutputCode",
                table: "Indicators",
                column: "SubOutputCode");

            migrationBuilder.CreateIndex(
                name: "IX_LogicalFramework_ProgramProjectID",
                table: "LogicalFramework",
                column: "ProgramProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_LogicalFrameworkIndicator_LogicalFrameworkCode",
                table: "LogicalFrameworkIndicator",
                column: "LogicalFrameworkCode");

            migrationBuilder.CreateIndex(
                name: "IX_Measure_IndicatorCode",
                table: "Measure",
                column: "IndicatorCode");

            migrationBuilder.CreateIndex(
                name: "IX_Measure_LogicalFrameworkIndicatorCode",
                table: "Measure",
                column: "LogicalFrameworkIndicatorCode");

            migrationBuilder.CreateIndex(
                name: "IX_Outcomes_FrameworkCode",
                table: "Outcomes",
                column: "FrameworkCode");

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_OutcomeCode",
                table: "Outputs",
                column: "OutcomeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Program_DonorCode",
                table: "Program",
                column: "DonorCode");

            migrationBuilder.CreateIndex(
                name: "IX_Program_MinistrieCode",
                table: "Program",
                column: "MinistrieCode");

            migrationBuilder.CreateIndex(
                name: "IX_Program_ProjectManagerCode",
                table: "Program",
                column: "ProjectManagerCode");

            migrationBuilder.CreateIndex(
                name: "IX_Program_RegionCode",
                table: "Program",
                column: "RegionCode");

            migrationBuilder.CreateIndex(
                name: "IX_Program_SuperVisorCode",
                table: "Program",
                column: "SuperVisorCode");

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
                name: "Assessment");

            migrationBuilder.DropTable(
                name: "Goal");

            migrationBuilder.DropTable(
                name: "Measure");

            migrationBuilder.DropTable(
                name: "Sector");

            migrationBuilder.DropTable(
                name: "Target");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Indicators");

            migrationBuilder.DropTable(
                name: "LogicalFrameworkIndicator");

            migrationBuilder.DropTable(
                name: "SubOutputs");

            migrationBuilder.DropTable(
                name: "LogicalFramework");

            migrationBuilder.DropTable(
                name: "Outputs");

            migrationBuilder.DropTable(
                name: "Program");

            migrationBuilder.DropTable(
                name: "Outcomes");

            migrationBuilder.DropTable(
                name: "Donor");

            migrationBuilder.DropTable(
                name: "Ministrie");

            migrationBuilder.DropTable(
                name: "ProjectManager");

            migrationBuilder.DropTable(
                name: "Region");

            migrationBuilder.DropTable(
                name: "SuperVisor");

            migrationBuilder.DropTable(
                name: "Framework");
        }
    }
}
