using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListeningTopicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ListeningExercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "VocabularyTopicId",
                table: "ListeningExercises",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "ListeningComprehensionQuestions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListeningExercises_VocabularyTopicId",
                table: "ListeningExercises",
                column: "VocabularyTopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListeningExercises_VocabularyTopicId",
                table: "ListeningExercises");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ListeningExercises");

            migrationBuilder.DropColumn(
                name: "VocabularyTopicId",
                table: "ListeningExercises");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "ListeningComprehensionQuestions");
        }
    }
}
