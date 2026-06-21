using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

public partial class AddAssistantSessionsV2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AssistantSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                Skill = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ResourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ResourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AssistantSessions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AssistantMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Text = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ClientRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                LatencyMs = table.Column<long>(type: "bigint", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssistantMessages", x => x.Id);
                table.ForeignKey("FK_AssistantMessages_AssistantSessions_SessionId", x => x.SessionId, "AssistantSessions", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_AssistantSessions_LearnerId_ExpiresAt", "AssistantSessions", new[] { "LearnerId", "ExpiresAt" });
        migrationBuilder.CreateIndex("IX_AssistantSessions_LearnerId_Skill_ResourceType_ResourceId_UpdatedAt", "AssistantSessions", new[] { "LearnerId", "Skill", "ResourceType", "ResourceId", "UpdatedAt" });
        migrationBuilder.CreateIndex("IX_AssistantMessages_SessionId_CreatedAt", "AssistantMessages", new[] { "SessionId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_AssistantMessages_SessionId_ClientRequestId_Role", "AssistantMessages", new[] { "SessionId", "ClientRequestId", "Role" }, unique: true, filter: "\"ClientRequestId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AssistantMessages");
        migrationBuilder.DropTable(name: "AssistantSessions");
    }
}
