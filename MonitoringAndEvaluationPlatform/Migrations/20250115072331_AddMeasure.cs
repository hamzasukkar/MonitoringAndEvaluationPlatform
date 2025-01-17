using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Indicators",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Concept",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "GAGRA",
                table: "Indicators",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GAGRR",
                table: "Indicators",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommon",
                table: "Indicators",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MethodOfComputation",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Indicators",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Target",
                table: "Indicators",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetYear",
                table: "Indicators",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Measure",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    IndicatorCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measure", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Measure_Indicators_IndicatorCode",
                        column: x => x.IndicatorCode,
                        principalTable: "Indicators",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Measure_IndicatorCode",
                table: "Measure",
                column: "IndicatorCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Measure");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "Concept",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "GAGRA",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "GAGRR",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "IsCommon",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "MethodOfComputation",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "Target",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "TargetYear",
                table: "Indicators");
        }
    }
}
