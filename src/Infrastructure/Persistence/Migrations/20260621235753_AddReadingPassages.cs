using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingPassages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReadingPassages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Topic = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingPassages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadingComprehensionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Options = table.Column<string>(type: "text", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    HintCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ReadingPassageId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingComprehensionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingComprehensionQuestions_ReadingPassages_ReadingPassag~",
                        column: x => x.ReadingPassageId,
                        principalTable: "ReadingPassages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadingGlossaryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Word = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Translation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExampleSentence = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReadingPassageId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingGlossaryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingGlossaryEntries_ReadingPassages_ReadingPassageId",
                        column: x => x.ReadingPassageId,
                        principalTable: "ReadingPassages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingComprehensionQuestions_ReadingPassageId",
                table: "ReadingComprehensionQuestions",
                column: "ReadingPassageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingGlossaryEntries_ReadingPassageId",
                table: "ReadingGlossaryEntries",
                column: "ReadingPassageId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingPassages_Level",
                table: "ReadingPassages",
                column: "Level");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReadingComprehensionQuestions");

            migrationBuilder.DropTable(
                name: "ReadingGlossaryEntries");

            migrationBuilder.DropTable(
                name: "ReadingPassages");
        }
    }
}
