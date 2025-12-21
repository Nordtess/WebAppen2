using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProfileCvFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove legacy columns that are no longer used (they previously duplicated AspNetUsers fields).
            migrationBuilder.DropColumn(
                name: "Namn",
                table: "Profiler");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Profiler");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Profiler");

            // Add CV-owned fields.
            migrationBuilder.AddColumn<string>(
                name: "Headline",
                table: "Profiler",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AboutMe",
                table: "Profiler",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImagePath",
                table: "Profiler",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkillsCsv",
                table: "Profiler",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationJson",
                table: "Profiler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedProjectsJson",
                table: "Profiler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Headline",
                table: "Profiler");

            migrationBuilder.DropColumn(
                name: "AboutMe",
                table: "Profiler");

            migrationBuilder.DropColumn(
                name: "ProfileImagePath",
                table: "Profiler");

            migrationBuilder.DropColumn(
                name: "SkillsCsv",
                table: "Profiler");

            migrationBuilder.DropColumn(
                name: "EducationJson",
                table: "Profiler");

            migrationBuilder.DropColumn(
                name: "SelectedProjectsJson",
                table: "Profiler");

            // Re-add legacy columns.
            migrationBuilder.AddColumn<string>(
                name: "Namn",
                table: "Profiler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Profiler",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Profiler",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
