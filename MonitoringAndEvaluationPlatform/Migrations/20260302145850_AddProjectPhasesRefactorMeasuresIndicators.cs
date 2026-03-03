using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectPhasesRefactorMeasuresIndicators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionPlans_Projects_ProjectID",
                table: "ActionPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Measures_Indicators_IndicatorCode",
                table: "Measures");

            migrationBuilder.DropTable(
                name: "ProjectIndicators");

            migrationBuilder.RenameColumn(
                name: "IndicatorCode",
                table: "Measures",
                newName: "ProjectPhaseId");

            migrationBuilder.RenameIndex(
                name: "IX_Measures_IndicatorCode",
                table: "Measures",
                newName: "IX_Measures_ProjectPhaseId");

            migrationBuilder.RenameColumn(
                name: "ProjectID",
                table: "ActionPlans",
                newName: "ProjectPhaseId");

            migrationBuilder.RenameIndex(
                name: "IX_ActionPlans_ProjectID",
                table: "ActionPlans",
                newName: "IX_ActionPlans_ProjectPhaseId");

            migrationBuilder.AddColumn<int>(
                name: "ProjectID",
                table: "Indicators",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectPhases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Budget = table.Column<double>(type: "float", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PhasePerformance = table.Column<double>(type: "float", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPhases_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_ProjectID",
                table: "Indicators",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhases_ProjectID",
                table: "ProjectPhases",
                column: "ProjectID");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionPlans_ProjectPhases_ProjectPhaseId",
                table: "ActionPlans",
                column: "ProjectPhaseId",
                principalTable: "ProjectPhases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Indicators_Projects_ProjectID",
                table: "Indicators",
                column: "ProjectID",
                principalTable: "Projects",
                principalColumn: "ProjectID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Measures_ProjectPhases_ProjectPhaseId",
                table: "Measures",
                column: "ProjectPhaseId",
                principalTable: "ProjectPhases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionPlans_ProjectPhases_ProjectPhaseId",
                table: "ActionPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Indicators_Projects_ProjectID",
                table: "Indicators");

            migrationBuilder.DropForeignKey(
                name: "FK_Measures_ProjectPhases_ProjectPhaseId",
                table: "Measures");

            migrationBuilder.DropTable(
                name: "ProjectPhases");

            migrationBuilder.DropIndex(
                name: "IX_Indicators_ProjectID",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "ProjectID",
                table: "Indicators");

            migrationBuilder.RenameColumn(
                name: "ProjectPhaseId",
                table: "Measures",
                newName: "IndicatorCode");

            migrationBuilder.RenameIndex(
                name: "IX_Measures_ProjectPhaseId",
                table: "Measures",
                newName: "IX_Measures_IndicatorCode");

            migrationBuilder.RenameColumn(
                name: "ProjectPhaseId",
                table: "ActionPlans",
                newName: "ProjectID");

            migrationBuilder.RenameIndex(
                name: "IX_ActionPlans_ProjectPhaseId",
                table: "ActionPlans",
                newName: "IX_ActionPlans_ProjectID");

            migrationBuilder.CreateTable(
                name: "ProjectIndicators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndicatorCode = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_ProjectIndicators_IndicatorCode",
                table: "ProjectIndicators",
                column: "IndicatorCode");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIndicators_ProjectId",
                table: "ProjectIndicators",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionPlans_Projects_ProjectID",
                table: "ActionPlans",
                column: "ProjectID",
                principalTable: "Projects",
                principalColumn: "ProjectID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Measures_Indicators_IndicatorCode",
                table: "Measures",
                column: "IndicatorCode",
                principalTable: "Indicators",
                principalColumn: "IndicatorCode",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
