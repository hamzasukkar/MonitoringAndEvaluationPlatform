using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class Models : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubOutput",
                table: "SubOutputs",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Output",
                table: "Outputs",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Outcome",
                table: "Outcomes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Indicator",
                table: "Indicators",
                newName: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "SubOutputs",
                newName: "SubOutput");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Outputs",
                newName: "Output");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Outcomes",
                newName: "Outcome");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Indicators",
                newName: "Indicator");
        }
    }
}
