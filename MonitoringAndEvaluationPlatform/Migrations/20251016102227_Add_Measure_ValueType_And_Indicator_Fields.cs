using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class Add_Measure_ValueType_And_Indicator_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ValueType",
                table: "Measures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "TargetYear",
                table: "Indicators",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Atif",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IndicatorImpact",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Trend",
                table: "Indicators",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueType",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "Atif",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "IndicatorImpact",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "Trend",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Indicators");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TargetYear",
                table: "Indicators",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
