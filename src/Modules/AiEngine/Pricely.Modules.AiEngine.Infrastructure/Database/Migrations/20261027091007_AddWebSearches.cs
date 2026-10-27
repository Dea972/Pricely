using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricely.Modules.AiEngine.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddWebSearches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "web_searches",
                schema: "aiengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    searched_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    searches = table.Column<int>(type: "integer", nullable: false),
                    results = table.Column<int>(type: "integer", nullable: false),
                    response_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    price_per_search = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_web_searches", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_web_searches_searched_on_utc",
                schema: "aiengine",
                table: "web_searches",
                column: "searched_on_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "web_searches",
                schema: "aiengine");
        }
    }
}
