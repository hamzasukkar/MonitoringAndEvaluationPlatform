using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectIdFromMeasures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Measures_Projects_ProjectID",
                table: "Measures");

            migrationBuilder.DropIndex(
                name: "IX_Measures_ProjectID",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "ProjectID",
                table: "Measures");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectID",
                table: "Measures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Measures_ProjectID",
                table: "Measures",
                column: "ProjectID");

            migrationBuilder.AddForeignKey(
                name: "FK_Measures_Projects_ProjectID",
                table: "Measures",
                column: "ProjectID",
                principalTable: "Projects",
                principalColumn: "ProjectID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
