using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableAndDatesToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: DateTime.UtcNow);

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAt",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RemovedAt",
                table: "Users");
        }
    }
}
