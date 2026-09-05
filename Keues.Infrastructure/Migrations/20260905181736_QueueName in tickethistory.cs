using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QueueNameintickethistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QueueId",
                table: "TicketHistories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketHistories_QueueId",
                table: "TicketHistories",
                column: "QueueId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketHistories_Queues_QueueId",
                table: "TicketHistories",
                column: "QueueId",
                principalTable: "Queues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketHistories_Queues_QueueId",
                table: "TicketHistories");

            migrationBuilder.DropIndex(
                name: "IX_TicketHistories_QueueId",
                table: "TicketHistories");

            migrationBuilder.DropColumn(
                name: "QueueId",
                table: "TicketHistories");
        }
    }
}
