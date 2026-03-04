using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class FixDonorCategoryDefaultValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set existing donors with invalid category (0) to UNOrganizations (1)
            migrationBuilder.Sql("UPDATE Donors SET donorCategory = 1 WHERE donorCategory = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Donors SET donorCategory = 0 WHERE donorCategory = 1");
        }
    }
}
