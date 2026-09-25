using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCollegeUserMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Colleges",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colleges_CreatedByUserId",
                table: "Colleges",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Colleges_AspNetUsers_CreatedByUserId",
                table: "Colleges",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colleges_AspNetUsers_CreatedByUserId",
                table: "Colleges");

            migrationBuilder.DropIndex(
                name: "IX_Colleges_CreatedByUserId",
                table: "Colleges");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Colleges");
        }
    }
}
