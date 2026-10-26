using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricely.Modules.Pricing.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "evidence_ai_rationale",
                schema: "pricing",
                table: "offers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_observed_title",
                schema: "pricing",
                table: "offers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "search_template",
                schema: "pricing",
                table: "merchants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "page_snapshots",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    content = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                    fetched_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_search_page = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_page_snapshots_merchants_merchant_id",
                        column: x => x.merchant_id,
                        principalSchema: "pricing",
                        principalTable: "merchants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_page_snapshots_merchant_id_fetched_on_utc",
                schema: "pricing",
                table: "page_snapshots",
                columns: new[] { "merchant_id", "fetched_on_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "page_snapshots",
                schema: "pricing");

            migrationBuilder.DropColumn(
                name: "evidence_ai_rationale",
                schema: "pricing",
                table: "offers");

            migrationBuilder.DropColumn(
                name: "evidence_observed_title",
                schema: "pricing",
                table: "offers");

            migrationBuilder.DropColumn(
                name: "search_template",
                schema: "pricing",
                table: "merchants");
        }
    }
}
