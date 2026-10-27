using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricely.Modules.Pricing.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPageAndDeclaredPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal[]>(
                name: "declared_prices",
                schema: "pricing",
                table: "page_snapshots",
                type: "numeric(18,2)[]",
                nullable: false,
                defaultValue: new decimal[0]);

            migrationBuilder.AddColumn<string>(
                name: "product_page_template",
                schema: "pricing",
                table: "merchants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "declared_prices",
                schema: "pricing",
                table: "page_snapshots");

            migrationBuilder.DropColumn(
                name: "product_page_template",
                schema: "pricing",
                table: "merchants");
        }
    }
}
