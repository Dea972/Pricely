using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricely.Modules.Pricing.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pricing");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "catalog_categories",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_products",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    brand = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    gtin = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    manufacturer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "collection_profiles",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_message_consumers",
                schema: "pricing",
                columns: table => new
                {
                    inbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_message_consumers", x => new { x.inbox_message_id, x.name });
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "merchants",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "citext", maxLength: 150, nullable: false),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message_consumers",
                schema: "pricing",
                columns: table => new
                {
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message_consumers", x => new { x.outbox_message_id, x.name });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "collection_profile_preferred_merchants",
                schema: "pricing",
                columns: table => new
                {
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_profile_preferred_merchants", x => new { x.collection_profile_id, x.merchant_id });
                    table.ForeignKey(
                        name: "fk_collection_profile_preferred_merchants_collection_profiles_",
                        column: x => x.collection_profile_id,
                        principalSchema: "pricing",
                        principalTable: "collection_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_collection_profile_preferred_merchants_merchants_merchant_id",
                        column: x => x.merchant_id,
                        principalSchema: "pricing",
                        principalTable: "merchants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offers",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_page = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    merchant_product_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confirmation_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    proposed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    evidence_ai_similarity = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    evidence_observed_brand = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    evidence_observed_gtin = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    evidence_observed_manufacturer_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_offers_merchants_merchant_id",
                        column: x => x.merchant_id,
                        principalSchema: "pricing",
                        principalTable: "merchants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_observations",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    availability = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_flagged_as_anomaly = table.Column<bool>(type: "boolean", nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_observations", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_observations_offers_offer_id",
                        column: x => x.offer_id,
                        principalSchema: "pricing",
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_products_product_category_id",
                schema: "pricing",
                table: "catalog_products",
                column: "product_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_profile_preferred_merchants_merchant_id",
                schema: "pricing",
                table: "collection_profile_preferred_merchants",
                column: "merchant_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_profiles_product_category_id",
                schema: "pricing",
                table: "collection_profiles",
                column: "product_category_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_merchants_name",
                schema: "pricing",
                table: "merchants",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offers_merchant_id",
                schema: "pricing",
                table: "offers",
                column: "merchant_id");

            migrationBuilder.CreateIndex(
                name: "ix_offers_product_id_merchant_id",
                schema: "pricing",
                table: "offers",
                columns: new[] { "product_id", "merchant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offers_status",
                schema: "pricing",
                table: "offers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_price_observations_offer_id_observed_on_utc",
                schema: "pricing",
                table: "price_observations",
                columns: new[] { "offer_id", "observed_on_utc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_categories",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "catalog_products",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "collection_profile_preferred_merchants",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "inbox_message_consumers",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "outbox_message_consumers",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "price_observations",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "collection_profiles",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "offers",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "merchants",
                schema: "pricing");
        }
    }
}
