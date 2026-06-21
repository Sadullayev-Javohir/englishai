using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VocabularyTopics_Level",
                table: "VocabularyTopics");

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "VocabularyTopics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTopics_Level_Sequence",
                table: "VocabularyTopics",
                columns: new[] { "Level", "Sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VocabularyTopics_Level_Sequence",
                table: "VocabularyTopics");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "VocabularyTopics");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTopics_Level",
                table: "VocabularyTopics",
                column: "Level");
        }
    }
}
