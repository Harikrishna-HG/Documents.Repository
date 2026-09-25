using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedDescriptionAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
           name: "Description",
           table: "Notice",
           type: "nvarchar(max)",
           nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Notice",
                type: "datetime2",
                nullable: false,
                defaultValue: DateTime.UtcNow);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                       name: "Description",
                       table: "Notice");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Notice");
        }
    }
}
