using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerErrorContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpectedAnswer",
                table: "LearnerErrorObservations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "LearnerErrorObservations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LearnerAnswer",
                table: "LearnerErrorObservations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prompt",
                table: "LearnerErrorObservations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "LearnerErrorObservations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "LearnerErrorObservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnerErrorObservations_Source_SourceId",
                table: "LearnerErrorObservations",
                columns: new[] { "Source", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LearnerErrorObservations_Source_SourceId",
                table: "LearnerErrorObservations");

            migrationBuilder.DropColumn(
                name: "ExpectedAnswer",
                table: "LearnerErrorObservations");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "LearnerErrorObservations");

            migrationBuilder.DropColumn(
                name: "LearnerAnswer",
                table: "LearnerErrorObservations");

            migrationBuilder.DropColumn(
                name: "Prompt",
                table: "LearnerErrorObservations");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "LearnerErrorObservations");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "LearnerErrorObservations");
        }
    }
}
