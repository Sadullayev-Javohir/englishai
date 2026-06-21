using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicWordImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "VocabularyTopicWords",
                type: "character varying(800)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageSource",
                table: "VocabularyTopicWords",
                type: "character varying(60)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageAttribution",
                table: "VocabularyTopicWords",
                type: "character varying(300)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageAttribution",
                table: "VocabularyTopicWords");

            migrationBuilder.DropColumn(
                name: "ImageSource",
                table: "VocabularyTopicWords");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "VocabularyTopicWords");
        }
    }
}
