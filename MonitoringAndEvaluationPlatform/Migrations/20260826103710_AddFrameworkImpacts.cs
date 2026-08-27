using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameworkImpacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FrameworkImpacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaselineYear = table.Column<int>(type: "int", nullable: false),
                    BaselineValue = table.Column<double>(type: "float", nullable: false),
                    TargetYear = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<double>(type: "float", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FrameworkCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkImpacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FrameworkImpacts_Frameworks_FrameworkCode",
                        column: x => x.FrameworkCode,
                        principalTable: "Frameworks",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FrameworkImpactIndicators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FrameworkImpactId = table.Column<int>(type: "int", nullable: false),
                    ImpactIndicatorId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkImpactIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FrameworkImpactIndicators_FrameworkImpacts_FrameworkImpactId",
                        column: x => x.FrameworkImpactId,
                        principalTable: "FrameworkImpacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FrameworkImpactIndicators_ImpactIndicators_ImpactIndicatorId",
                        column: x => x.ImpactIndicatorId,
                        principalTable: "ImpactIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkImpactIndicators_FrameworkImpactId_ImpactIndicatorId",
                table: "FrameworkImpactIndicators",
                columns: new[] { "FrameworkImpactId", "ImpactIndicatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkImpactIndicators_ImpactIndicatorId",
                table: "FrameworkImpactIndicators",
                column: "ImpactIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkImpacts_FrameworkCode",
                table: "FrameworkImpacts",
                column: "FrameworkCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FrameworkImpactIndicators");

            migrationBuilder.DropTable(
                name: "FrameworkImpacts");
        }
    }
}
