using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizedUsersAndGroupsToCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CounterId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CounterId",
                table: "UserGroups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CounterId",
                table: "Users",
                column: "CounterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_CounterId",
                table: "UserGroups",
                column: "CounterId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_Counters_CounterId",
                table: "UserGroups",
                column: "CounterId",
                principalTable: "Counters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Counters_CounterId",
                table: "Users",
                column: "CounterId",
                principalTable: "Counters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_Counters_CounterId",
                table: "UserGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Counters_CounterId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CounterId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserGroups_CounterId",
                table: "UserGroups");

            migrationBuilder.DropColumn(
                name: "CounterId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CounterId",
                table: "UserGroups");
        }
    }
}
