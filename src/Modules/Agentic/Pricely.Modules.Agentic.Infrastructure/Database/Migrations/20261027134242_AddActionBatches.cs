using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricely.Modules.Agentic.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddActionBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "batch_id",
                schema: "agentic",
                table: "pending_actions",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<int>(
                name: "batch_sequence",
                schema: "agentic",
                table: "pending_actions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // An action proposed before batches is a batch of its own.
            migrationBuilder.Sql("UPDATE agentic.pending_actions SET batch_id = id;");

            migrationBuilder.CreateIndex(
                name: "ix_pending_actions_batch_id",
                schema: "agentic",
                table: "pending_actions",
                column: "batch_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pending_actions_batch_id",
                schema: "agentic",
                table: "pending_actions");

            migrationBuilder.DropColumn(
                name: "batch_id",
                schema: "agentic",
                table: "pending_actions");

            migrationBuilder.DropColumn(
                name: "batch_sequence",
                schema: "agentic",
                table: "pending_actions");
        }
    }
}
