using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicSpeakingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TopicSpeakingProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    VocabularyTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpokenSeconds = table.Column<double>(type: "double precision", nullable: false),
                    LearnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicSpeakingProgress", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicSpeakingProgress_LearnerId_LearnedAt",
                table: "TopicSpeakingProgress",
                columns: new[] { "LearnerId", "LearnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TopicSpeakingProgress_LearnerId_VocabularyTopicId",
                table: "TopicSpeakingProgress",
                columns: new[] { "LearnerId", "VocabularyTopicId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicSpeakingProgress");
        }
    }
}
