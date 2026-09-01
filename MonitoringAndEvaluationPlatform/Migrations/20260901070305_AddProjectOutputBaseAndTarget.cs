using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectOutputBaseAndTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BaseValue",
                table: "ProjectOutputs",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TargetValue",
                table: "ProjectOutputs",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseValue",
                table: "ProjectOutputs");

            migrationBuilder.DropColumn(
                name: "TargetValue",
                table: "ProjectOutputs");
        }
    }
}
