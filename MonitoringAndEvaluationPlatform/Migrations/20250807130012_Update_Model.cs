using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class Update_Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DisbursementPerformance",
                table: "Sectors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Sectors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Sectors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "IndicatorsPerformance",
                table: "Sectors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "ImpactAssessment",
                table: "Donors",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "FieldMonitoring",
                table: "Donors",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "DisbursementPerformance",
                table: "Donors",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<double>(
                name: "IndicatorsPerformance",
                table: "Donors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisbursementPerformance",
                table: "Sectors");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Sectors");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Sectors");

            migrationBuilder.DropColumn(
                name: "IndicatorsPerformance",
                table: "Sectors");

            migrationBuilder.DropColumn(
                name: "IndicatorsPerformance",
                table: "Donors");

            migrationBuilder.AlterColumn<int>(
                name: "ImpactAssessment",
                table: "Donors",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "FieldMonitoring",
                table: "Donors",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "DisbursementPerformance",
                table: "Donors",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
