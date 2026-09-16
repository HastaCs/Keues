using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LocationInUserGroupRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_Locations_LocationId",
                table: "UserGroups");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "UserGroups",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_Locations_LocationId",
                table: "UserGroups",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserGroups_Locations_LocationId",
                table: "UserGroups");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "UserGroups",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_UserGroups_Locations_LocationId",
                table: "UserGroups",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");
        }
    }
}
