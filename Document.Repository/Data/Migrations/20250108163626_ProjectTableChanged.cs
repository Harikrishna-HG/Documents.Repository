using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Document.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectTableChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectFiles_Projects_ProjectId",
                table: "ProjectFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectFiles",
                table: "ProjectFiles");

            migrationBuilder.RenameTable(
                name: "ProjectFiles",
                newName: "Documents");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Projects",
                newName: "Thubmnail");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectFiles_ProjectId",
                table: "Documents",
                newName: "IX_Documents_ProjectId");

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "Students",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Projects",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Abstract",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Collection",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Documents",
                table: "Documents",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Projects_ProjectId",
                table: "Documents",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Projects_ProjectId",
                table: "Documents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Documents",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Abstract",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Collection",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Projects");

            migrationBuilder.RenameTable(
                name: "Documents",
                newName: "ProjectFiles");

            migrationBuilder.RenameColumn(
                name: "Thubmnail",
                table: "Projects",
                newName: "Description");

            migrationBuilder.RenameIndex(
                name: "IX_Documents_ProjectId",
                table: "ProjectFiles",
                newName: "IX_ProjectFiles_ProjectId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectFiles",
                table: "ProjectFiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFiles_Projects_ProjectId",
                table: "ProjectFiles",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
