using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class changeMinistryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Program_Ministrie_MinistrieCode",
                table: "Program");

            migrationBuilder.DropTable(
                name: "Ministrie");

            migrationBuilder.RenameColumn(
                name: "MinistrieCode",
                table: "Program",
                newName: "MinistryCode");

            migrationBuilder.RenameIndex(
                name: "IX_Program_MinistrieCode",
                table: "Program",
                newName: "IX_Program_MinistryCode");

            migrationBuilder.RenameColumn(
                name: "MinistrieName",
                table: "AspNetUsers",
                newName: "MinistryName");

            migrationBuilder.CreateTable(
                name: "Ministry",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinistryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ministry", x => x.Code);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Program_Ministry_MinistryCode",
                table: "Program",
                column: "MinistryCode",
                principalTable: "Ministry",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Program_Ministry_MinistryCode",
                table: "Program");

            migrationBuilder.DropTable(
                name: "Ministry");

            migrationBuilder.RenameColumn(
                name: "MinistryCode",
                table: "Program",
                newName: "MinistrieCode");

            migrationBuilder.RenameIndex(
                name: "IX_Program_MinistryCode",
                table: "Program",
                newName: "IX_Program_MinistrieCode");

            migrationBuilder.RenameColumn(
                name: "MinistryName",
                table: "AspNetUsers",
                newName: "MinistrieName");

            migrationBuilder.CreateTable(
                name: "Ministrie",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisbursementPerformance = table.Column<int>(type: "int", nullable: false),
                    FieldMonitoring = table.Column<int>(type: "int", nullable: false),
                    ImpactAssessment = table.Column<int>(type: "int", nullable: false),
                    Partner = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ministrie", x => x.Code);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Program_Ministrie_MinistrieCode",
                table: "Program",
                column: "MinistrieCode",
                principalTable: "Ministrie",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
