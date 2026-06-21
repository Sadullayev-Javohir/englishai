using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Data recovery (no schema change): resets every lesson stuck on the terminal
    /// <c>Unavailable</c> (2) transcript status back to <c>Pending</c> (0). Before this release a
    /// single transient yt-dlp failure (tool missing in the container, rate-limit, network) marked
    /// the lesson terminally "no transcript" and it was never retried - so otherwise-captioned
    /// videos showed "no interactive transcript" forever. The fill path now distinguishes a genuine
    /// no-captions result from a provider failure (see TranscriptFetchResult); resetting lets these
    /// poisoned lessons re-fetch on the next open. Genuinely caption-less videos simply re-settle to
    /// Unavailable, so the reset is self-healing and safe.
    /// </summary>
    public partial class ResetPoisonedTranscriptStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE ""VideoLessons"" SET ""TranscriptStatus"" = 0 WHERE ""TranscriptStatus"" = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // One-way data recovery: there is no meaningful inverse (we cannot know which lessons
            // were genuinely Unavailable beforehand), so the down migration is intentionally a no-op.
        }
    }
}
