using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListeningExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListeningExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Transcript = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Topic = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListeningExercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListeningComprehensionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Options = table.Column<string>(type: "text", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    HintCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ListeningExerciseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListeningComprehensionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListeningComprehensionQuestions_ListeningExercises_Listenin~",
                        column: x => x.ListeningExerciseId,
                        principalTable: "ListeningExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListeningComprehensionQuestions_ListeningExerciseId",
                table: "ListeningComprehensionQuestions",
                column: "ListeningExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ListeningExercises_Level",
                table: "ListeningExercises",
                column: "Level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListeningComprehensionQuestions");

            migrationBuilder.DropTable(
                name: "ListeningExercises");
        }
    }
}
