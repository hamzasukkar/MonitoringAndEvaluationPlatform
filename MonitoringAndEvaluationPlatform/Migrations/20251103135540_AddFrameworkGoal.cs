using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameworkGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FrameworkGoals",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartingYear = table.Column<int>(type: "int", nullable: false),
                    BaseValueForStartingYear = table.Column<double>(type: "float", nullable: false),
                    CurrentYear = table.Column<int>(type: "int", nullable: false),
                    BaseValueForCurrentYear = table.Column<double>(type: "float", nullable: false),
                    TargetYear = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<double>(type: "float", nullable: false),
                    FrameworkCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkGoals", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FrameworkGoals_Frameworks_FrameworkCode",
                        column: x => x.FrameworkCode,
                        principalTable: "Frameworks",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkGoals_FrameworkCode",
                table: "FrameworkGoals",
                column: "FrameworkCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FrameworkGoals");
        }
    }
}
