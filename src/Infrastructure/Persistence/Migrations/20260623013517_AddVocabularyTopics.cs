using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularyTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VocabularyTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TitleUz = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Passage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyTopicWords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Translation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExampleSentence = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VocabularyTopicId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyTopicWords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyTopicWords_VocabularyTopics_VocabularyTopicId",
                        column: x => x.VocabularyTopicId,
                        principalTable: "VocabularyTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTopics_Level",
                table: "VocabularyTopics",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTopics_Slug",
                table: "VocabularyTopics",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTopicWords_VocabularyTopicId",
                table: "VocabularyTopicWords",
                column: "VocabularyTopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularyTopicWords");

            migrationBuilder.DropTable(
                name: "VocabularyTopics");
        }
    }
}
