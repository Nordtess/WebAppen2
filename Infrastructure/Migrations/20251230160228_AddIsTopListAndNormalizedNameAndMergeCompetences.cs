using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTopListAndNormalizedNameAndMergeCompetences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old unique index on Name if it exists (case-insensitive duplicates will be handled via NormalizedName)
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Kompetenskatalog_Name' AND object_id = OBJECT_ID('Kompetenskatalog'))
DROP INDEX [IX_Kompetenskatalog_Name] ON [Kompetenskatalog];");

            // Add IsTopList if it does not already exist (current DB is missing it)
            migrationBuilder.AddColumn<bool>(
                name: "IsTopList",
                table: "Kompetenskatalog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Add normalized name (computed, persisted)
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Kompetenskatalog",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                computedColumnSql: "UPPER(LTRIM(RTRIM([Name])))",
                stored: true);

            // Backfill flags and merge duplicates on normalized name
            migrationBuilder.Sql(@"
-- Normalize top-list flag from old 'Topplista' category rows
UPDATE k
SET IsTopList = CASE WHEN k.Category = 'Topplista' THEN 1 ELSE k.IsTopList END
FROM Kompetenskatalog k;

-- Snapshot current competencies with normalized name
IF OBJECT_ID('tempdb..#comp') IS NOT NULL DROP TABLE #comp;
SELECT Id,
       Name,
       Category,
       SortOrder,
       IsTopList,
       UPPER(LTRIM(RTRIM(Name))) AS NormalizedName
INTO #comp
FROM Kompetenskatalog;

IF OBJECT_ID('tempdb..#masters') IS NOT NULL DROP TABLE #masters;
SELECT NormalizedName,
       MIN(Id) AS MasterId,
       MAX(CASE WHEN IsTopList = 1 OR Category = 'Topplista' THEN 1 ELSE 0 END) AS HasTop,
       MIN(CASE WHEN Category <> 'Topplista' THEN Category END) AS AnyCategory,
       MIN(CASE WHEN Category <> 'Topplista' THEN SortOrder END) AS AnySortOrder
INTO #masters
FROM #comp
GROUP BY NormalizedName;

-- Remove duplicate user-competence rows before remapping
;WITH uc_dedup AS (
    SELECT uc.Id,
           ROW_NUMBER() OVER (PARTITION BY uc.UserId, c.NormalizedName ORDER BY uc.Id) AS rn
    FROM AnvandarKompetenser uc
    JOIN #comp c ON uc.CompetenceId = c.Id
)
DELETE uc
FROM AnvandarKompetenser uc
JOIN uc_dedup d ON uc.Id = d.Id
WHERE d.rn > 1;

-- Move FK references to master
UPDATE uc
SET CompetenceId = m.MasterId
FROM AnvandarKompetenser uc
JOIN #comp c ON uc.CompetenceId = c.Id
JOIN #masters m ON c.NormalizedName = m.NormalizedName;

-- Update master rows with proper category/top-list/sort order
UPDATE k
SET IsTopList = CASE WHEN m.HasTop = 1 THEN 1 ELSE 0 END,
    Category = COALESCE(m.AnyCategory, k.Category),
    SortOrder = COALESCE(m.AnySortOrder, k.SortOrder)
FROM Kompetenskatalog k
JOIN #masters m ON k.Id = m.MasterId;

-- Remove duplicates, keeping master
DELETE k
FROM Kompetenskatalog k
JOIN #masters m ON k.NormalizedName = m.NormalizedName
WHERE k.Id <> m.MasterId;

-- Ensure seed top-list flags are set (idempotent)
UPDATE Kompetenskatalog
SET IsTopList = 1
WHERE Id IN (1,2,3,4,5,6,7,8,9,10,11,12);
");

            migrationBuilder.CreateIndex(
                name: "IX_Kompetenskatalog_NormalizedName",
                table: "Kompetenskatalog",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kompetenskatalog_NormalizedName",
                table: "Kompetenskatalog");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Kompetenskatalog");

            migrationBuilder.DropColumn(
                name: "IsTopList",
                table: "Kompetenskatalog");

            // Recreate legacy unique index on Name for rollback safety
            migrationBuilder.CreateIndex(
                name: "IX_Kompetenskatalog_Name",
                table: "Kompetenskatalog",
                column: "Name",
                unique: true);
        }
    }
}
