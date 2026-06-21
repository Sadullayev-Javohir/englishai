using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDemographics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcquisitionSource",
                table: "UserAccounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcquisitionSourceOther",
                table: "UserAccounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "UserAccounts",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DemographicsCompletedAt",
                table: "UserAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "UserAccounts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcquisitionSource",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "AcquisitionSourceOther",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "DemographicsCompletedAt",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "UserAccounts");
        }
    }
}
