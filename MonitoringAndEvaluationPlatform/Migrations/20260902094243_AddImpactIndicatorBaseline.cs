using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddImpactIndicatorBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BaselineValue",
                table: "ImpactIndicators",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BaselineYear",
                table: "ImpactIndicators",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaselineValue",
                table: "ImpactIndicators");

            migrationBuilder.DropColumn(
                name: "BaselineYear",
                table: "ImpactIndicators");
        }
    }
}
