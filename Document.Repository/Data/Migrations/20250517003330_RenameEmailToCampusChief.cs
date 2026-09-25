using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmailToCampusChief : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Colleges",
                newName: "CampusChief");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CampusChief",
                table: "Colleges",
                newName: "Email");
        }
    }
}
