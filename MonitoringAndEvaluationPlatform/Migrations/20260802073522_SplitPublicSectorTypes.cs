using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class SplitPublicSectorTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only touch databases that already have the pre-split seed data;
            // fresh databases are seeded directly with the new list by DbInitializer.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM PublicSectorTypes WHERE EN_Name = N'Electricity and Water')
BEGIN
    UPDATE PublicSectorTypes SET EN_Name = N'Electricity', AR_Name = N'الكهرباء' WHERE EN_Name = N'Electricity and Water';
    INSERT INTO PublicSectorTypes (EN_Name, AR_Name) VALUES (N'Water Resources', N'الموارد المائية');
END

IF EXISTS (SELECT 1 FROM PublicSectorTypes WHERE EN_Name = N'Services')
BEGIN
    UPDATE PublicSectorTypes SET EN_Name = N'Health', AR_Name = N'الصحة' WHERE EN_Name = N'Services';
    INSERT INTO PublicSectorTypes (EN_Name, AR_Name) VALUES (N'Education', N'التعليم');
    INSERT INTO PublicSectorTypes (EN_Name, AR_Name) VALUES (N'Culture', N'الثقافة');
END

IF EXISTS (SELECT 1 FROM PublicSectorTypes)
   AND NOT EXISTS (SELECT 1 FROM PublicSectorTypes WHERE EN_Name = N'Communications and Information Technology')
BEGIN
    INSERT INTO PublicSectorTypes (EN_Name, AR_Name) VALUES (N'Communications and Information Technology', N'الاتصالات وتكنولوجيا المعلومات');
    INSERT INTO PublicSectorTypes (EN_Name, AR_Name) VALUES (N'Tourism', N'السياحة');
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM PublicSectorTypes WHERE EN_Name IN (N'Water Resources', N'Education', N'Culture', N'Communications and Information Technology', N'Tourism');

IF EXISTS (SELECT 1 FROM PublicSectorTypes WHERE EN_Name = N'Electricity')
BEGIN
    UPDATE PublicSectorTypes SET EN_Name = N'Electricity and Water', AR_Name = N'الكهرباء والماء' WHERE EN_Name = N'Electricity';
END

IF EXISTS (SELECT 1 FROM PublicSectorTypes WHERE EN_Name = N'Health')
BEGIN
    UPDATE PublicSectorTypes SET EN_Name = N'Services', AR_Name = N'الخدمات' WHERE EN_Name = N'Health';
END
");
        }
    }
}
