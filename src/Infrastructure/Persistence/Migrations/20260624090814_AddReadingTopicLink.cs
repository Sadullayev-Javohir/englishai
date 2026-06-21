using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingTopicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ReadingPassages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "VocabularyTopicId",
                table: "ReadingPassages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "ReadingComprehensionQuestions",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingPassages_VocabularyTopicId",
                table: "ReadingPassages",
                column: "VocabularyTopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadingPassages_VocabularyTopicId",
                table: "ReadingPassages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ReadingPassages");

            migrationBuilder.DropColumn(
                name: "VocabularyTopicId",
                table: "ReadingPassages");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "ReadingComprehensionQuestions");
        }
    }
}
