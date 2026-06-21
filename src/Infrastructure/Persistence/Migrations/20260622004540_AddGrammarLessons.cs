using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGrammarLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GrammarLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ContextIntro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExplanationCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarLessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrammarApplicationTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetSkill = table.Column<int>(type: "integer", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GrammarLessonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarApplicationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarApplicationTasks_GrammarLessons_GrammarLessonId",
                        column: x => x.GrammarLessonId,
                        principalTable: "GrammarLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GrammarExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Options = table.Column<string>(type: "text", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    HintCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    GrammarLessonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarExercises_GrammarLessons_GrammarLessonId",
                        column: x => x.GrammarLessonId,
                        principalTable: "GrammarLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarApplicationTasks_GrammarLessonId",
                table: "GrammarApplicationTasks",
                column: "GrammarLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_GrammarExercises_GrammarLessonId",
                table: "GrammarExercises",
                column: "GrammarLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_GrammarLessons_Category",
                table: "GrammarLessons",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_GrammarLessons_Level",
                table: "GrammarLessons",
                column: "Level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrammarApplicationTasks");

            migrationBuilder.DropTable(
                name: "GrammarExercises");

            migrationBuilder.DropTable(
                name: "GrammarLessons");
        }
    }
}
