using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoTranscriptStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TranscriptStatus",
                table: "VideoLessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing lessons that already have transcript segments are Available (1), not Pending (0);
            // only lessons with no segments stay Pending so they get a (re)fill on the next open.
            migrationBuilder.Sql(
                @"UPDATE ""VideoLessons"" SET ""TranscriptStatus"" = 1 WHERE ""Id"" IN (
                    SELECT DISTINCT ""VideoLessonId"" FROM ""VideoTranscriptSegments"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TranscriptStatus",
                table: "VideoLessons");
        }
    }
}
