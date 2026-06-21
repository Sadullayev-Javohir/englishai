using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    YouTubeVideoId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Channel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Topic = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoLessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VideoComprehensionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Options = table.Column<string>(type: "text", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    HintCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    VideoLessonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoComprehensionQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoComprehensionQuestions_VideoLessons_VideoLessonId",
                        column: x => x.VideoLessonId,
                        principalTable: "VideoLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoTranscriptSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartSeconds = table.Column<double>(type: "double precision", nullable: false),
                    EndSeconds = table.Column<double>(type: "double precision", nullable: false),
                    EnglishText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UzbekTranslation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VideoLessonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTranscriptSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoTranscriptSegments_VideoLessons_VideoLessonId",
                        column: x => x.VideoLessonId,
                        principalTable: "VideoLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoComprehensionQuestions_VideoLessonId",
                table: "VideoComprehensionQuestions",
                column: "VideoLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoLessons_Level",
                table: "VideoLessons",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptSegments_VideoLessonId",
                table: "VideoTranscriptSegments",
                column: "VideoLessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoComprehensionQuestions");

            migrationBuilder.DropTable(
                name: "VideoTranscriptSegments");

            migrationBuilder.DropTable(
                name: "VideoLessons");
        }
    }
}
