using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTopListAndMergeCompetences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTopList",
                table: "Kompetenskatalog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
-- Välj master per namn (case-insensitivt) och markera Topplista
;WITH cte AS (
    SELECT Id, Name, Category, SortOrder, LOWER(LTRIM(RTRIM(Name))) AS NormName
    FROM Kompetenskatalog
), masters AS (
    SELECT NormName,
           MIN(Id) AS MasterId,
           MAX(CASE WHEN Category = 'Topplista' THEN 1 ELSE 0 END) AS HasTop,
           MIN(CASE WHEN Category <> 'Topplista' THEN Category ELSE NULL END) AS AnyCategory
    FROM cte
    GROUP BY NormName
)
-- Sätt Topplista-flaggan på master-rader
UPDATE k
SET IsTopList = CASE WHEN m.HasTop = 1 THEN 1 ELSE 0 END
FROM Kompetenskatalog k
JOIN masters m ON k.Id = m.MasterId;

-- Flytta kategori till icke-Topplista om sådan finns
UPDATE k
SET Category = COALESCE(m.AnyCategory, k.Category)
FROM Kompetenskatalog k
JOIN masters m ON k.Id = m.MasterId
WHERE m.AnyCategory IS NOT NULL;

-- Flytta relationer till master-id
UPDATE uc
SET CompetenceId = m.MasterId
FROM AnvandarKompetenser uc
JOIN cte c ON uc.CompetenceId = c.Id
JOIN masters m ON c.NormName = m.NormName;

-- Ta bort dubletter (behåll master)
DELETE k
FROM Kompetenskatalog k
JOIN masters m ON LOWER(LTRIM(RTRIM(k.Name))) = m.NormName
WHERE k.Id <> m.MasterId;

-- Säkerställ Topplista-flagga på seedade topprader (om de redan finns)
UPDATE Kompetenskatalog
SET IsTopList = 1
WHERE Id IN (1,2,3,4,5,6,7,8,9,10,11,12);
");

            migrationBuilder.CreateIndex(
                name: "IX_Kompetenskatalog_Name",
                table: "Kompetenskatalog",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kompetenskatalog_Name",
                table: "Kompetenskatalog");

            migrationBuilder.DropColumn(
                name: "IsTopList",
                table: "Kompetenskatalog");
        }
    }
}
