using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWritingTopicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Guidance",
                table: "WritingTasks",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WritingTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "VocabularyTopicId",
                table: "WritingTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WritingTasks_VocabularyTopicId",
                table: "WritingTasks",
                column: "VocabularyTopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WritingTasks_VocabularyTopicId",
                table: "WritingTasks");

            migrationBuilder.DropColumn(
                name: "Guidance",
                table: "WritingTasks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WritingTasks");

            migrationBuilder.DropColumn(
                name: "VocabularyTopicId",
                table: "WritingTasks");
        }
    }
}
