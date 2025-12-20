using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNormalizedNamesAndDeactivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstNameNormalized",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastNameNormalized",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeactivated",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FirstNameNormalized",
                table: "AspNetUsers",
                column: "FirstNameNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LastNameNormalized",
                table: "AspNetUsers",
                column: "LastNameNormalized");

            migrationBuilder.Sql(@"
UPDATE [AspNetUsers]
SET
    [FirstNameNormalized] = UPPER(LTRIM(RTRIM(ISNULL([FirstName], '')))),
    [LastNameNormalized]  = UPPER(LTRIM(RTRIM(ISNULL([LastName], ''))))
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FirstNameNormalized",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LastNameNormalized",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstNameNormalized",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastNameNormalized",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsDeactivated",
                table: "AspNetUsers");
        }
    }
}
