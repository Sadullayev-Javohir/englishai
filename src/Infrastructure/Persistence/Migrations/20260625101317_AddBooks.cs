using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TitleUz = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Author = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Synopsis = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Topic = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CoverImageQuery = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CoverImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CoverAttribution = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookSectionScores",
                columns: table => new
                {
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookProgressId = table.Column<Guid>(type: "uuid", nullable: false),
                    BestCorrectCount = table.Column<int>(type: "integer", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    AchievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookSectionScores", x => new { x.BookProgressId, x.SectionId });
                    table.ForeignKey(
                        name: "FK_BookSectionScores_BookProgress_BookProgressId",
                        column: x => x.BookProgressId,
                        principalTable: "BookProgress",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookSections_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookComprehensionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Options = table.Column<string>(type: "text", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    Explanation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BookSectionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookComprehensionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookComprehensionQuestions_BookSections_BookSectionId",
                        column: x => x.BookSectionId,
                        principalTable: "BookSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookComprehensionQuestions_BookSectionId",
                table: "BookComprehensionQuestions",
                column: "BookSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BookProgress_LearnerId",
                table: "BookProgress",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookProgress_LearnerId_BookId",
                table: "BookProgress",
                columns: new[] { "LearnerId", "BookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_Level",
                table: "Books",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_BookSections_BookId_Order",
                table: "BookSections",
                columns: new[] { "BookId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookComprehensionQuestions");

            migrationBuilder.DropTable(
                name: "BookSectionScores");

            migrationBuilder.DropTable(
                name: "BookSections");

            migrationBuilder.DropTable(
                name: "BookProgress");

            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
