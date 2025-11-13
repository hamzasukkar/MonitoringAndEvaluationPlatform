using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameworkGoalYearlyValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FrameworkGoalYearlyValues",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FrameworkGoalID = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    ActualValue = table.Column<double>(type: "float", nullable: false),
                    DateRecorded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkGoalYearlyValues", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FrameworkGoalYearlyValues_FrameworkGoals_FrameworkGoalID",
                        column: x => x.FrameworkGoalID,
                        principalTable: "FrameworkGoals",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkGoalYearlyValues_FrameworkGoalID_Year",
                table: "FrameworkGoalYearlyValues",
                columns: new[] { "FrameworkGoalID", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FrameworkGoalYearlyValues");
        }
    }
}
