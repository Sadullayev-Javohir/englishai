using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicCompletionRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicCompletionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    VocabularyTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    MasteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicCompletionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopicModuleScores",
                columns: table => new
                {
                    Module = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TopicCompletionRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    AchievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicModuleScores", x => new { x.TopicCompletionRecordId, x.Module });
                    table.ForeignKey(
                        name: "FK_TopicModuleScores_TopicCompletionRecords_TopicCompletionRec~",
                        column: x => x.TopicCompletionRecordId,
                        principalTable: "TopicCompletionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicCompletionRecords_LearnerId_MasteredAt",
                table: "TopicCompletionRecords",
                columns: new[] { "LearnerId", "MasteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TopicCompletionRecords_LearnerId_VocabularyTopicId",
                table: "TopicCompletionRecords",
                columns: new[] { "LearnerId", "VocabularyTopicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicModuleScores");

            migrationBuilder.DropTable(
                name: "TopicCompletionRecords");
        }
    }
}
