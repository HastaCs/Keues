using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemovedAtToUserGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAt",
                table: "UserGroups",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemovedAt",
                table: "UserGroups");
        }
    }
}
