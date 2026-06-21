using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveLearningGoalToLearnerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LearningGoal",
                table: "UserAccounts");

            migrationBuilder.AddColumn<int>(
                name: "LearningGoal",
                table: "LearnerProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LearningGoal",
                table: "LearnerProfiles");

            migrationBuilder.AddColumn<int>(
                name: "LearningGoal",
                table: "UserAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
