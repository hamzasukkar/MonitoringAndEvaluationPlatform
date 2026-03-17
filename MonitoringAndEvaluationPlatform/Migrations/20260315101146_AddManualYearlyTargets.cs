using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddManualYearlyTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ManualYearlyTargets",
                table: "FrameworkGoals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FrameworkGoalManualExpectedTargets",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FrameworkGoalID = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    ExpectedTargetValue = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkGoalManualExpectedTargets", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FrameworkGoalManualExpectedTargets_FrameworkGoals_FrameworkGoalID",
                        column: x => x.FrameworkGoalID,
                        principalTable: "FrameworkGoals",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkGoalManualExpectedTargets_FrameworkGoalID_Year",
                table: "FrameworkGoalManualExpectedTargets",
                columns: new[] { "FrameworkGoalID", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FrameworkGoalManualExpectedTargets");

            migrationBuilder.DropColumn(
                name: "ManualYearlyTargets",
                table: "FrameworkGoals");
        }
    }
}
