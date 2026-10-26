using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricely.Modules.Pricing.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class DropCatalogCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_categories",
                schema: "pricing");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
