using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pricely.Modules.Users.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "users");

            migrationBuilder.CreateTable(
                name: "inbox_message_consumers",
                schema: "users",
                columns: table => new
                {
                    inbox_message_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_message_consumers", x => new { x.inbox_message_id, x.name });
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", maxLength: 2000, nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message_consumers",
                schema: "users",
                columns: table => new
                {
                    outbox_message_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message_consumers", x => new { x.outbox_message_id, x.name });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", maxLength: 2000, nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "users",
                columns: table => new
                {
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ak_permission_code", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "users",
                columns: table => new
                {
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    identity_id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "users",
                columns: table => new
                {
                    permission_code = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    role_name = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.permission_code, x.role_name });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permissions_code",
                        column: x => x.permission_code,
                        principalSchema: "users",
                        principalTable: "permissions",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_name",
                        column: x => x.role_name,
                        principalSchema: "users",
                        principalTable: "roles",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                schema: "users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    permission_code = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_permissions", x => new { x.user_id, x.permission_code });
                    table.ForeignKey(
                        name: "fk_user_permissions_permission_permission_code",
                        column: x => x.permission_code,
                        principalSchema: "users",
                        principalTable: "permissions",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "users",
                columns: table => new
                {
                    role_name = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.role_name, x.user_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_roles_name",
                        column: x => x.role_name,
                        principalSchema: "users",
                        principalTable: "roles",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "users",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "permissions",
                columns: new[] { "code", "name" },
                values: new object[,]
                {
                    { "admin:access", "Admin Access" },
                    { "admin:settings", "Admin Settings" },
                    { "users:delete", "Delete Users" },
                    { "users:read", "Read Users" },
                    { "users:write", "Write Users" }
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "roles",
                column: "name",
                values: new object[]
                {
                    "Administrator",
                    "Custom",
                    "Member"
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "role_permissions",
                columns: new[] { "permission_code", "role_name" },
                values: new object[,]
                {
                    { "admin:access", "Administrator" },
                    { "admin:settings", "Administrator" },
                    { "users:delete", "Administrator" },
                    { "users:read", "Administrator" },
                    { "users:read", "Member" },
                    { "users:write", "Administrator" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_name",
                schema: "users",
                table: "role_permissions",
                column: "role_name");

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_permission_code",
                schema: "users",
                table: "user_permissions",
                column: "permission_code");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id",
                schema: "users",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "users",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_identity_id",
                schema: "users",
                table: "users",
                column: "identity_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_message_consumers",
                schema: "users");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "users");

            migrationBuilder.DropTable(
                name: "outbox_message_consumers",
                schema: "users");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "users");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "users");

            migrationBuilder.DropTable(
                name: "user_permissions",
                schema: "users");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "users");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "users");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "users");

            migrationBuilder.DropTable(
                name: "users",
                schema: "users");
        }
    }
}
