using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CurriculumV4Backfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LexicalCategory",
                table: "VocabularyTopicWords",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Register",
                table: "VocabularyTopicWords",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsageNote",
                table: "VocabularyTopicWords",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurriculumGenerationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SubjectKey = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookId = table.Column<Guid>(type: "uuid", nullable: true),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    PromptVersion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseOwner = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    ValidationJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumGenerationItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumGenerationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Provider = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumGenerationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopicSpeakingBlueprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Objective = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PrimaryGrammarFocus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReviewGrammarFocus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PriorityWordsJson = table.Column<string>(type: "jsonb", nullable: false),
                    QuestionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RubricJson = table.Column<string>(type: "jsonb", nullable: false),
                    CurriculumVersion = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicSpeakingBlueprints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumGenerationItems_RunId_Module_SubjectKey",
                table: "CurriculumGenerationItems",
                columns: new[] { "RunId", "Module", "SubjectKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumGenerationItems_RunId_Status_NextAttemptAt",
                table: "CurriculumGenerationItems",
                columns: new[] { "RunId", "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumGenerationRuns_Version_Status",
                table: "CurriculumGenerationRuns",
                columns: new[] { "Version", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TopicSpeakingBlueprints_TopicId",
                table: "TopicSpeakingBlueprints",
                column: "TopicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumGenerationItems");

            migrationBuilder.DropTable(
                name: "CurriculumGenerationRuns");

            migrationBuilder.DropTable(
                name: "TopicSpeakingBlueprints");

            migrationBuilder.DropColumn(
                name: "LexicalCategory",
                table: "VocabularyTopicWords");

            migrationBuilder.DropColumn(
                name: "Register",
                table: "VocabularyTopicWords");

            migrationBuilder.DropColumn(
                name: "UsageNote",
                table: "VocabularyTopicWords");
        }
    }
}
