using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistGrammarRuleCardContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CuratedFormulas",
                table: "GrammarLessons",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "CuratedSummaryUz",
                table: "GrammarLessons",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuratedTitleUz",
                table: "GrammarLessons",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GrammarCuratedRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HeadingUz = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BodyUz = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    GrammarLessonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarCuratedRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarCuratedRules_GrammarLessons_GrammarLessonId",
                        column: x => x.GrammarLessonId,
                        principalTable: "GrammarLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarCuratedRules_GrammarLessonId",
                table: "GrammarCuratedRules",
                column: "GrammarLessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrammarCuratedRules");

            migrationBuilder.DropColumn(
                name: "CuratedFormulas",
                table: "GrammarLessons");

            migrationBuilder.DropColumn(
                name: "CuratedSummaryUz",
                table: "GrammarLessons");

            migrationBuilder.DropColumn(
                name: "CuratedTitleUz",
                table: "GrammarLessons");
        }
    }
}
