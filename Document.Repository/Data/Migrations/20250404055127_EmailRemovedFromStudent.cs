using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class EmailRemovedFromStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Students");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
