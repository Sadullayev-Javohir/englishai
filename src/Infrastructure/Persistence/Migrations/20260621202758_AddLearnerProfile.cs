using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearnerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallLevel = table.Column<int>(type: "integer", nullable: false),
                    ConfirmationTestPassedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnerErrorObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Skill = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LearnerProfileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerErrorObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerErrorObservations_LearnerProfiles_LearnerProfileId",
                        column: x => x.LearnerProfileId,
                        principalTable: "LearnerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnerSkillActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Skill = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LearnerProfileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerSkillActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerSkillActivities_LearnerProfiles_LearnerProfileId",
                        column: x => x.LearnerProfileId,
                        principalTable: "LearnerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearnerSkillSeeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Skill = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    LearnerProfileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerSkillSeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnerSkillSeeds_LearnerProfiles_LearnerProfileId",
                        column: x => x.LearnerProfileId,
                        principalTable: "LearnerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearnerErrorObservations_LearnerProfileId",
                table: "LearnerErrorObservations",
                column: "LearnerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerErrorObservations_OccurredAt",
                table: "LearnerErrorObservations",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerProfiles_LearnerId",
                table: "LearnerProfiles",
                column: "LearnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnerSkillActivities_LearnerProfileId",
                table: "LearnerSkillActivities",
                column: "LearnerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerSkillActivities_OccurredAt",
                table: "LearnerSkillActivities",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerSkillSeeds_LearnerProfileId",
                table: "LearnerSkillSeeds",
                column: "LearnerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearnerErrorObservations");

            migrationBuilder.DropTable(
                name: "LearnerSkillActivities");

            migrationBuilder.DropTable(
                name: "LearnerSkillSeeds");

            migrationBuilder.DropTable(
                name: "LearnerProfiles");
        }
    }
}
