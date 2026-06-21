using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptWords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoTranscriptWords",
                columns: table => new
                {
                    TranscriptSegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    StartSeconds = table.Column<double>(type: "double precision", nullable: false),
                    EndSeconds = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTranscriptWords", x => new { x.TranscriptSegmentId, x.Id });
                    table.ForeignKey(
                        name: "FK_VideoTranscriptWords_VideoTranscriptSegments_TranscriptSegm~",
                        column: x => x.TranscriptSegmentId,
                        principalTable: "VideoTranscriptSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Existing transcripts were stored as arbitrary per-event chunks with no word timing.
            // Clearing them lets the lazy-fill repopulate each lesson with the new sentence lines +
            // per-word timing on next open (transcripts are all caption-derived, never hand-curated).
            migrationBuilder.Sql("DELETE FROM \"VideoTranscriptSegments\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoTranscriptWords");
        }
    }
}
