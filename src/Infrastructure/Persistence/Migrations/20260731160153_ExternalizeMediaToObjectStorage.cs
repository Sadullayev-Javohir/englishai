using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExternalizeMediaToObjectStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "UserAvatars",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "UserAvatars",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectETag",
                table: "UserAvatars",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "UserAvatars",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "UserAvatars",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StoredAt",
                table: "UserAvatars",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "TopicImages",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "TopicImages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectETag",
                table: "TopicImages",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "TopicImages",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "TopicImages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StoredAt",
                table: "TopicImages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "AudioContent",
                table: "ListeningAudioClips",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea");

            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "ListeningAudioClips",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "ListeningAudioClips",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ObjectETag",
                table: "ListeningAudioClips",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "ListeningAudioClips",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "ListeningAudioClips",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StoredAt",
                table: "ListeningAudioClips",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "ObjectETag",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "StoredAt",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "ObjectETag",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "StoredAt",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "ListeningAudioClips");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "ListeningAudioClips");

            migrationBuilder.DropColumn(
                name: "ObjectETag",
                table: "ListeningAudioClips");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "ListeningAudioClips");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "ListeningAudioClips");

            migrationBuilder.DropColumn(
                name: "StoredAt",
                table: "ListeningAudioClips");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "UserAvatars",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "TopicImages",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "AudioContent",
                table: "ListeningAudioClips",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);
        }
    }
}
