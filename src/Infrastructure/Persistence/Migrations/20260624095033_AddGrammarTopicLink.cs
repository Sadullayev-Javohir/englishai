using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrammarTopicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "GrammarLessons",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GrammarFocusCode",
                table: "GrammarLessons",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "GrammarLessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "VocabularyTopicId",
                table: "GrammarLessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "GrammarExercises",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrammarLessons_VocabularyTopicId",
                table: "GrammarLessons",
                column: "VocabularyTopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GrammarLessons_VocabularyTopicId",
                table: "GrammarLessons");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "GrammarLessons");

            migrationBuilder.DropColumn(
                name: "GrammarFocusCode",
                table: "GrammarLessons");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GrammarLessons");

            migrationBuilder.DropColumn(
                name: "VocabularyTopicId",
                table: "GrammarLessons");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "GrammarExercises");
        }
    }
}
