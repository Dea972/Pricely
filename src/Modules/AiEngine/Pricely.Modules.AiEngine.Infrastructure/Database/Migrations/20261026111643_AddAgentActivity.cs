using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricely.Modules.AiEngine.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agent_name",
                schema: "aiengine",
                table: "llm_calls",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agent_activities",
                schema: "aiengine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    feature_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    agent_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tool_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_read_only_tool = table.Column<bool>(type: "boolean", nullable: false),
                    was_approved = table.Column<bool>(type: "boolean", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    action_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_delay_ms = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_activities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_activities_agent_name_tool_name",
                schema: "aiengine",
                table: "agent_activities",
                columns: new[] { "agent_name", "tool_name" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_activities_occurred_on_utc",
                schema: "aiengine",
                table: "agent_activities",
                column: "occurred_on_utc");

            migrationBuilder.CreateIndex(
                name: "ix_agent_activities_session_id",
                schema: "aiengine",
                table: "agent_activities",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_activities",
                schema: "aiengine");

            migrationBuilder.DropColumn(
                name: "agent_name",
                schema: "aiengine",
                table: "llm_calls");
        }
    }
}
