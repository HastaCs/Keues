using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizedCountersInUserAndUserGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "CounterUser",
                columns: table => new
                {
                    AuthorizedCountersId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorizedUsersId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterUser", x => new { x.AuthorizedCountersId, x.AuthorizedUsersId });
                    table.ForeignKey(
                        name: "FK_CounterUser_Counters_AuthorizedCountersId",
                        column: x => x.AuthorizedCountersId,
                        principalTable: "Counters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CounterUser_Users_AuthorizedUsersId",
                        column: x => x.AuthorizedUsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CounterUserGroup",
                columns: table => new
                {
                    AuthorizedCountersId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorizedUserGroupsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounterUserGroup", x => new { x.AuthorizedCountersId, x.AuthorizedUserGroupsId });
                    table.ForeignKey(
                        name: "FK_CounterUserGroup_Counters_AuthorizedCountersId",
                        column: x => x.AuthorizedCountersId,
                        principalTable: "Counters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CounterUserGroup_UserGroups_AuthorizedUserGroupsId",
                        column: x => x.AuthorizedUserGroupsId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CounterUser_AuthorizedUsersId",
                table: "CounterUser",
                column: "AuthorizedUsersId");

            migrationBuilder.CreateIndex(
                name: "IX_CounterUserGroup_AuthorizedUserGroupsId",
                table: "CounterUserGroup",
                column: "AuthorizedUserGroupsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CounterUser");

            migrationBuilder.DropTable(
                name: "CounterUserGroup");

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
    }
}
