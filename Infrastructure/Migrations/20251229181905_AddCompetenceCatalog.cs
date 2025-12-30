using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetenceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnvandarKompetenser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompetenceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnvandarKompetenser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kompetenskatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kompetenskatalog", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Kompetenskatalog",
                columns: new[] { "Id", "Category", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Topplista", "C#", 0 },
                    { 2, "Topplista", ".NET", 1 },
                    { 3, "Topplista", "ASP.NET Core", 2 },
                    { 4, "Topplista", "MVC", 3 },
                    { 5, "Topplista", "EF Core", 4 },
                    { 6, "Topplista", "LINQ", 5 },
                    { 7, "Topplista", "SQL", 6 },
                    { 8, "Topplista", "Git", 7 },
                    { 9, "Topplista", "Docker", 8 },
                    { 10, "Topplista", "Azure", 9 },
                    { 11, "Topplista", "Linux", 10 },
                    { 12, "Topplista", "REST API", 11 },
                    { 13, "Programmeringsspråk", "C#", 0 },
                    { 14, "Programmeringsspråk", "Java", 1 },
                    { 15, "Programmeringsspråk", "Python", 2 },
                    { 16, "Programmeringsspråk", "JavaScript", 3 },
                    { 17, "Programmeringsspråk", "TypeScript", 4 },
                    { 18, "Programmeringsspråk", "SQL", 5 },
                    { 19, "Programmeringsspråk", "HTML", 6 },
                    { 20, "Programmeringsspråk", "CSS", 7 },
                    { 21, "Programmeringsspråk", "Bash", 8 },
                    { 22, "Backend", ".NET", 0 },
                    { 23, "Backend", "ASP.NET Core", 1 },
                    { 24, "Backend", "MVC", 2 },
                    { 25, "Backend", "Web API", 3 },
                    { 26, "Backend", "EF Core", 4 },
                    { 27, "Backend", "LINQ", 5 },
                    { 28, "Backend", "SignalR", 6 },
                    { 29, "Frontend", "React", 0 },
                    { 30, "Frontend", "Vue", 1 },
                    { 31, "Frontend", "Angular", 2 },
                    { 32, "Frontend", "Vite", 3 },
                    { 33, "Frontend", "Tailwind", 4 },
                    { 34, "Frontend", "Bootstrap", 5 },
                    { 35, "Databaser", "SQL Server", 0 },
                    { 36, "Databaser", "PostgreSQL", 1 },
                    { 37, "Databaser", "MySQL", 2 },
                    { 38, "Databaser", "SQLite", 3 },
                    { 39, "Databaser", "MongoDB", 4 },
                    { 40, "Databaser", "Redis", 5 },
                    { 41, "DevOps & Drift", "Git", 0 },
                    { 42, "DevOps & Drift", "GitHub", 1 },
                    { 43, "DevOps & Drift", "CI/CD", 2 },
                    { 44, "DevOps & Drift", "Docker", 3 },
                    { 45, "DevOps & Drift", "Kubernetes", 4 },
                    { 46, "DevOps & Drift", "Linux", 5 },
                    { 47, "DevOps & Drift", "Nginx", 6 },
                    { 48, "DevOps & Drift", "Azure", 7 },
                    { 49, "DevOps & Drift", "AWS", 8 },
                    { 50, "Test & Kvalitet", "xUnit", 0 },
                    { 51, "Test & Kvalitet", "NUnit", 1 },
                    { 52, "Test & Kvalitet", "Integration Tests", 2 },
                    { 53, "Test & Kvalitet", "Unit Tests", 3 },
                    { 54, "Test & Kvalitet", "Logging", 4 },
                    { 55, "Test & Kvalitet", "Serilog", 5 },
                    { 56, "Säkerhet", "OWASP", 0 },
                    { 57, "Säkerhet", "HTTPS/TLS", 1 },
                    { 58, "Säkerhet", "JWT", 2 },
                    { 59, "Säkerhet", "OAuth2", 3 },
                    { 60, "Arkitektur & Metoder", "Clean Architecture", 0 },
                    { 61, "Arkitektur & Metoder", "SOLID", 1 },
                    { 62, "Arkitektur & Metoder", "DDD", 2 },
                    { 63, "Arkitektur & Metoder", "Agile", 3 },
                    { 64, "Arkitektur & Metoder", "Scrum", 4 },
                    { 65, "Arkitektur & Metoder", "TDD", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnvandarKompetenser_UserId_CompetenceId",
                table: "AnvandarKompetenser",
                columns: new[] { "UserId", "CompetenceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnvandarKompetenser");

            migrationBuilder.DropTable(
                name: "Kompetenskatalog");
        }
    }
}
