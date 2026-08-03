using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlatformVersionId",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlatformVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_PlatformVersionId",
                table: "Requests",
                column: "PlatformVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformVersions_VersionNumber",
                table: "PlatformVersions",
                column: "VersionNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_PlatformVersions_PlatformVersionId",
                table: "Requests",
                column: "PlatformVersionId",
                principalTable: "PlatformVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_PlatformVersions_PlatformVersionId",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "PlatformVersions");

            migrationBuilder.DropIndex(
                name: "IX_Requests_PlatformVersionId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "PlatformVersionId",
                table: "Requests");
        }
    }
}
