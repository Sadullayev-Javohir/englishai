using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VocabularyItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Translation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExampleSentence = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Schedule_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Schedule_LearnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Schedule_NextReviewAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Schedule_Stage = table.Column<int>(type: "integer", nullable: false),
                    Schedule_FailCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_LearnerId",
                table: "VocabularyItems",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyItems_Schedule_NextReviewAt",
                table: "VocabularyItems",
                column: "Schedule_NextReviewAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularyItems");
        }
    }
}
