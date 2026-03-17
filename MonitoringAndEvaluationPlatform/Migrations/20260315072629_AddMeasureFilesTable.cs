using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddMeasureFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Measures");

            migrationBuilder.CreateTable(
                name: "MeasureFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeasureCode = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasureFiles_Measures_MeasureCode",
                        column: x => x.MeasureCode,
                        principalTable: "Measures",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeasureFiles_MeasureCode",
                table: "MeasureFiles",
                column: "MeasureCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeasureFiles");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Measures",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
