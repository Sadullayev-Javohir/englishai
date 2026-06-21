using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionAndLearningEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Competitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostLearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Settings_SlideDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Settings_AutoAdvance = table.Column<bool>(type: "boolean", nullable: false),
                    Settings_ShuffleSlides = table.Column<bool>(type: "boolean", nullable: false),
                    Settings_AllowLateJoin = table.Column<bool>(type: "boolean", nullable: false),
                    Settings_QuestionsPerTopic = table.Column<int>(type: "integer", nullable: false),
                    Settings_ShowLiveLeaderboard = table.Column<bool>(type: "boolean", nullable: false),
                    Settings_BasePointsPerCorrect = table.Column<int>(type: "integer", nullable: false),
                    TopicIds = table.Column<string>(type: "text", nullable: false),
                    CurrentSlideIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: -1),
                    AccessCodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AccessCodeSalt = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Competitions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CompetitionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsHost = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionParticipants", x => x.Id);
                    table.ForeignKey("FK_CompetitionParticipants_Competitions_CompetitionId", x => x.CompetitionId,
                        "Competitions", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionSlides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Options = table.Column<string>(type: "text", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionSlides", x => x.Id);
                    table.ForeignKey("FK_CompetitionSlides_Competitions_CompetitionId", x => x.CompetitionId,
                        "Competitions", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlideId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOptionIndex = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionAnswers", x => x.Id);
                    table.ForeignKey("FK_CompetitionAnswers_CompetitionParticipants_ParticipantId", x => x.ParticipantId,
                        "CompetitionParticipants", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_CompetitionAnswers_ParticipantId", "CompetitionAnswers", "ParticipantId");
            migrationBuilder.CreateIndex("IX_CompetitionParticipants_CompetitionId", "CompetitionParticipants", "CompetitionId");
            migrationBuilder.CreateIndex("IX_CompetitionSlides_CompetitionId", "CompetitionSlides", "CompetitionId");

            migrationBuilder.AddColumn<int>(
                name: "Schedule_LastHintCount",
                table: "VocabularyItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Schedule_LastResponseLatencyMs",
                table: "VocabularyItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Schedule_ReviewCount",
                table: "VocabularyItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CompetitionAnswers");
            migrationBuilder.DropTable(name: "CompetitionSlides");
            migrationBuilder.DropTable(name: "CompetitionParticipants");
            migrationBuilder.DropTable(name: "Competitions");

            migrationBuilder.DropColumn(
                name: "Schedule_LastHintCount",
                table: "VocabularyItems");

            migrationBuilder.DropColumn(
                name: "Schedule_LastResponseLatencyMs",
                table: "VocabularyItems");

            migrationBuilder.DropColumn(
                name: "Schedule_ReviewCount",
                table: "VocabularyItems");
        }
    }
}
