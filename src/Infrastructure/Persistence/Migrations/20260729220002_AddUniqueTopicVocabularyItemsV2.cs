using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTopicVocabularyItemsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "VocabularyItems" duplicate
                USING "VocabularyItems" retained
                WHERE duplicate."LearnerId" = retained."LearnerId"
                  AND duplicate."SourceTopicId" = retained."SourceTopicId"
                  AND lower(duplicate."Word") = lower(retained."Word")
                  AND duplicate."SourceTopicId" IS NOT NULL
                  AND (duplicate."CreatedAt", duplicate."Id") > (retained."CreatedAt", retained."Id");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_LearnerId_SourceTopicId_Word",
                table: "VocabularyItems",
                columns: new[] { "LearnerId", "SourceTopicId", "Word" },
                unique: true,
                filter: "\"SourceTopicId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VocabularyItems_LearnerId_SourceTopicId_Word",
                table: "VocabularyItems");
        }
    }
}
