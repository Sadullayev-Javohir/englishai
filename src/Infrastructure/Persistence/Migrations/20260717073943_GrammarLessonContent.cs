using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GrammarLessonContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GrammarCommonMistake",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    GrammarLessonId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarCommonMistake", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarCommonMistake_GrammarLessons_GrammarLessonId",
                        column: x => x.GrammarLessonId,
                        principalTable: "GrammarLessons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GrammarExample",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    English = table.Column<string>(type: "text", nullable: false),
                    Uzbek = table.Column<string>(type: "text", nullable: false),
                    GrammarLessonId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarExample", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarExample_GrammarLessons_GrammarLessonId",
                        column: x => x.GrammarLessonId,
                        principalTable: "GrammarLessons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarCommonMistake_GrammarLessonId",
                table: "GrammarCommonMistake",
                column: "GrammarLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_GrammarExample_GrammarLessonId",
                table: "GrammarExample",
                column: "GrammarLessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrammarCommonMistake");

            migrationBuilder.DropTable(
                name: "GrammarExample");
        }
    }
}
