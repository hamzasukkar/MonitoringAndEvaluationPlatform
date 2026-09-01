using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectOutputActualImpact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectOutputActualImpacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectOutputId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    DateRecorded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOutputActualImpacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectOutputActualImpacts_ProjectOutputs_ProjectOutputId",
                        column: x => x.ProjectOutputId,
                        principalTable: "ProjectOutputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutputActualImpacts_ProjectOutputId_Year",
                table: "ProjectOutputActualImpacts",
                columns: new[] { "ProjectOutputId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectOutputActualImpacts");
        }
    }
}
