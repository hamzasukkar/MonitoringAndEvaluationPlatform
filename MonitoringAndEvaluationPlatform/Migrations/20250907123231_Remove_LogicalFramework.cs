using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class Remove_LogicalFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logicalMeasures");

            migrationBuilder.DropTable(
                name: "logicalFrameworkIndicators");

            migrationBuilder.DropTable(
                name: "logicalFrameworks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "logicalFrameworks",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Performance = table.Column<double>(type: "float", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
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
                name: "logicalFrameworkIndicators",
                columns: table => new
                {
                    IndicatorCode = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogicalFrameworkCode = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Concept = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GAGRA = table.Column<double>(type: "float", nullable: false),
                    GAGRR = table.Column<double>(type: "float", nullable: false),
                    IsCommon = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Performance = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<int>(type: "int", nullable: false),
                    TargetYear = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false)
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
                name: "logicalMeasures",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogicalFrameworkIndicatorIndicatorCode = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false)
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
        }
    }
}
