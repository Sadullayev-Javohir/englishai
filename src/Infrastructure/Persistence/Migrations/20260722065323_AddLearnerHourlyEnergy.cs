using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearnerHourlyEnergy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Energy",
                table: "LearnerPoints",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnergyRefilledAt",
                table: "LearnerPoints",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "date_trunc('hour', now() at time zone 'utc') at time zone 'utc'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Energy",
                table: "LearnerPoints");

            migrationBuilder.DropColumn(
                name: "EnergyRefilledAt",
                table: "LearnerPoints");
        }
    }
}
