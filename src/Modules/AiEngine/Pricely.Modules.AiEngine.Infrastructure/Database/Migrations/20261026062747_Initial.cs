using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricely.Modules.AiEngine.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "aiengine");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "inbox_message_consumers",
                schema: "aiengine",
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
                schema: "aiengine",
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
                name: "llm_calls",
                schema: "aiengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    called_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    input_cached_tokens = table.Column<int>(type: "integer", nullable: false),
                    input_cache_creation_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_reasoning_tokens = table.Column<int>(type: "integer", nullable: false),
                    response_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    input_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    cached_input_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    cache_write_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    output_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_llm_calls", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "models",
                schema: "aiengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    provider_model_id = table.Column<string>(type: "citext", maxLength: 150, nullable: false),
                    label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_models", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feature_settings",
                schema: "aiengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "citext", maxLength: 100, nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feature_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_feature_settings_models_model_id",
                        column: x => x.model_id,
                        principalSchema: "aiengine",
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "model_prices",
                schema: "aiengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    input_per_million = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cached_input_per_million = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    cache_write_per_million = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    output_per_million = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recorded_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_model_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_model_prices_models_model_id",
                        column: x => x.model_id,
                        principalSchema: "aiengine",
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_feature_settings_feature_code",
                schema: "aiengine",
                table: "feature_settings",
                column: "feature_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_feature_settings_model_id",
                schema: "aiengine",
                table: "feature_settings",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_llm_calls_called_on_utc",
                schema: "aiengine",
                table: "llm_calls",
                column: "called_on_utc");

            migrationBuilder.CreateIndex(
                name: "ix_llm_calls_feature_code",
                schema: "aiengine",
                table: "llm_calls",
                column: "feature_code");

            migrationBuilder.CreateIndex(
                name: "ix_llm_calls_session_id",
                schema: "aiengine",
                table: "llm_calls",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_model_prices_model_id_valid_from",
                schema: "aiengine",
                table: "model_prices",
                columns: new[] { "model_id", "valid_from" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_models_provider_provider_model_id",
                schema: "aiengine",
                table: "models",
                columns: new[] { "provider", "provider_model_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feature_settings",
                schema: "aiengine");

            migrationBuilder.DropTable(
                name: "inbox_message_consumers",
                schema: "aiengine");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "aiengine");

            migrationBuilder.DropTable(
                name: "llm_calls",
                schema: "aiengine");

            migrationBuilder.DropTable(
                name: "model_prices",
                schema: "aiengine");

            migrationBuilder.DropTable(
                name: "models",
                schema: "aiengine");
        }
    }
}
