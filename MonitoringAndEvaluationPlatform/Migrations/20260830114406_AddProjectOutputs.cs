using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectOutputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectOutputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOutputs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectOutputFrameworks",
                columns: table => new
                {
                    FrameworksCode = table.Column<int>(type: "int", nullable: false),
                    ProjectOutputId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOutputFrameworks", x => new { x.FrameworksCode, x.ProjectOutputId });
                    table.ForeignKey(
                        name: "FK_ProjectOutputFrameworks_Frameworks_FrameworksCode",
                        column: x => x.FrameworksCode,
                        principalTable: "Frameworks",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectOutputFrameworks_ProjectOutputs_ProjectOutputId",
                        column: x => x.ProjectOutputId,
                        principalTable: "ProjectOutputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectOutputImpactIndicators",
                columns: table => new
                {
                    ImpactIndicatorsId = table.Column<int>(type: "int", nullable: false),
                    ProjectOutputsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOutputImpactIndicators", x => new { x.ImpactIndicatorsId, x.ProjectOutputsId });
                    table.ForeignKey(
                        name: "FK_ProjectOutputImpactIndicators_ImpactIndicators_ImpactIndicatorsId",
                        column: x => x.ImpactIndicatorsId,
                        principalTable: "ImpactIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectOutputImpactIndicators_ProjectOutputs_ProjectOutputsId",
                        column: x => x.ProjectOutputsId,
                        principalTable: "ProjectOutputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectOutputMinistries",
                columns: table => new
                {
                    MinistriesCode = table.Column<int>(type: "int", nullable: false),
                    ProjectOutputId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOutputMinistries", x => new { x.MinistriesCode, x.ProjectOutputId });
                    table.ForeignKey(
                        name: "FK_ProjectOutputMinistries_Ministries_MinistriesCode",
                        column: x => x.MinistriesCode,
                        principalTable: "Ministries",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectOutputMinistries_ProjectOutputs_ProjectOutputId",
                        column: x => x.ProjectOutputId,
                        principalTable: "ProjectOutputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutputFrameworks_ProjectOutputId",
                table: "ProjectOutputFrameworks",
                column: "ProjectOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutputImpactIndicators_ProjectOutputsId",
                table: "ProjectOutputImpactIndicators",
                column: "ProjectOutputsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutputMinistries_ProjectOutputId",
                table: "ProjectOutputMinistries",
                column: "ProjectOutputId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectOutputFrameworks");

            migrationBuilder.DropTable(
                name: "ProjectOutputImpactIndicators");

            migrationBuilder.DropTable(
                name: "ProjectOutputMinistries");

            migrationBuilder.DropTable(
                name: "ProjectOutputs");
        }
    }
}
