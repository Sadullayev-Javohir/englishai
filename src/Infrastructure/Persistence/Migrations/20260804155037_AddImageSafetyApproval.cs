using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageSafetyApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SafetyCheckedAt",
                table: "TopicImages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyModelVersion",
                table: "TopicImages",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyReasons",
                table: "TopicImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SafetyStatus",
                table: "TopicImages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SafetyCheckedAt",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "SafetyModelVersion",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "SafetyReasons",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "SafetyStatus",
                table: "TopicImages");
        }
    }
}
