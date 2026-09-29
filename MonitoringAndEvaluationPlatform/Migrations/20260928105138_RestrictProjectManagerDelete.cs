using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RestrictProjectManagerDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectManagers_ProjectManagerCode",
                table: "Projects");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectManagers_ProjectManagerCode",
                table: "Projects",
                column: "ProjectManagerCode",
                principalTable: "ProjectManagers",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectManagers_ProjectManagerCode",
                table: "Projects");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectManagers_ProjectManagerCode",
                table: "Projects",
                column: "ProjectManagerCode",
                principalTable: "ProjectManagers",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
