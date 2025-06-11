using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialLocationData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Governorates",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { "GOV001", "Governorate A" },
                    { "GOV002", "Governorate B" }
                });

            migrationBuilder.InsertData(
                table: "Districts",
                columns: new[] { "Code", "GovernorateCode", "Name" },
                values: new object[,]
                {
                    { "D001", "GOV001", "District 1" },
                    { "D002", "GOV001", "District 2" },
                    { "D003", "GOV002", "District 3" }
                });

            migrationBuilder.InsertData(
                table: "SubDistricts",
                columns: new[] { "Code", "DistrictCode", "Name" },
                values: new object[,]
                {
                    { "SD001", "D001", "SubDistrict 1" },
                    { "SD002", "D002", "SubDistrict 2" }
                });

            migrationBuilder.InsertData(
                table: "Communities",
                columns: new[] { "Code", "Name", "SubDistrictCode" },
                values: new object[,]
                {
                    { "C001", "Community 1", "SD001" },
                    { "C002", "Community 2", "SD002" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Communities",
                keyColumn: "Code",
                keyValue: "C001");

            migrationBuilder.DeleteData(
                table: "Communities",
                keyColumn: "Code",
                keyValue: "C002");

            migrationBuilder.DeleteData(
                table: "Districts",
                keyColumn: "Code",
                keyValue: "D003");

            migrationBuilder.DeleteData(
                table: "Governorates",
                keyColumn: "Code",
                keyValue: "GOV002");

            migrationBuilder.DeleteData(
                table: "SubDistricts",
                keyColumn: "Code",
                keyValue: "SD001");

            migrationBuilder.DeleteData(
                table: "SubDistricts",
                keyColumn: "Code",
                keyValue: "SD002");

            migrationBuilder.DeleteData(
                table: "Districts",
                keyColumn: "Code",
                keyValue: "D001");

            migrationBuilder.DeleteData(
                table: "Districts",
                keyColumn: "Code",
                keyValue: "D002");

            migrationBuilder.DeleteData(
                table: "Governorates",
                keyColumn: "Code",
                keyValue: "GOV001");
        }
    }
}
