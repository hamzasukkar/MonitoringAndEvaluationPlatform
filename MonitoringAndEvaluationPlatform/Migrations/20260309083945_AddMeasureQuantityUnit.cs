using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddMeasureQuantityUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Quantity",
                table: "Measures",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Measures",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Measures");
        }
    }
}
