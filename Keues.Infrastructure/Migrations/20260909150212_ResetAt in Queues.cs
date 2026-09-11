using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResetAtinQueues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastResetAt",
                table: "Queues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ResetAt",
                table: "Queues",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastResetAt",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "ResetAt",
                table: "Queues");
        }
    }
}
