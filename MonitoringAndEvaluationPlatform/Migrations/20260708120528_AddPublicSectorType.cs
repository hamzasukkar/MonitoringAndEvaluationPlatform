using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicSectorType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PublicSectorTypeCode",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicSectorTypes",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EN_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AR_Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicSectorTypes", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PublicSectorTypeCode",
                table: "Projects",
                column: "PublicSectorTypeCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_PublicSectorTypes_PublicSectorTypeCode",
                table: "Projects",
                column: "PublicSectorTypeCode",
                principalTable: "PublicSectorTypes",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_PublicSectorTypes_PublicSectorTypeCode",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "PublicSectorTypes");

            migrationBuilder.DropIndex(
                name: "IX_Projects_PublicSectorTypeCode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PublicSectorTypeCode",
                table: "Projects");
        }
    }
}
