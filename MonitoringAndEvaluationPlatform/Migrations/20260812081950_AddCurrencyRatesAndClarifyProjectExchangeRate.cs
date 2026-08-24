using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyRatesAndClarifyProjectExchangeRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "Projects",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RateToSyp = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "CurrencyRates",
                columns: new[] { "Code", "NameAr", "NameEn", "RateToSyp", "Symbol", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { "EUR", "يورو", "Euro", 14000m, "€", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "SYP", "ليرة سورية", "Syrian Pound", 1m, "SYP ", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "USD", "دولار أمريكي", "US Dollar", 13000m, "$", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            // Project.ExchangeRate changes meaning here: it now holds "SYP per 1 unit of the
            // project's own Currency". On SYP projects it used to hold "SYP per USD" purely to
            // drive a USD-equivalent display box — a value that is meaningless (and dangerous,
            // since 1 SYP is not 13000 SYP) under the new definition. Clear those rows; the
            // display box now reads the platform rate from CurrencyRates instead.
            // Non-SYP rows are left untouched: they are all NULL today, because the rate input
            // was only ever shown for SYP.
            migrationBuilder.Sql(
                "UPDATE Projects SET ExchangeRate = NULL, ExchangeRateDate = NULL WHERE Currency = 'SYP';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyRates");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "Projects",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);
        }
    }
}
