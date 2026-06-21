using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDatabaseHotPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VocabularyItems_LearnerId",
                table: "VocabularyItems");

            migrationBuilder.DropIndex(
                name: "IX_VideoLessons_Level",
                table: "VideoLessons");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_LearnerId_CreatedAt",
                table: "VocabularyItems",
                columns: new[] { "LearnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoLessons_Status_Level_CreatedAt",
                table: "VideoLessons",
                columns: new[] { "Status", "Level", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoLessons_YouTubeVideoId",
                table: "VideoLessons",
                column: "YouTubeVideoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VocabularyItems_LearnerId_CreatedAt",
                table: "VocabularyItems");

            migrationBuilder.DropIndex(
                name: "IX_VideoLessons_Status_Level_CreatedAt",
                table: "VideoLessons");

            migrationBuilder.DropIndex(
                name: "IX_VideoLessons_YouTubeVideoId",
                table: "VideoLessons");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_LearnerId",
                table: "VocabularyItems",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoLessons_Level",
                table: "VideoLessons",
                column: "Level");
        }
    }
}
