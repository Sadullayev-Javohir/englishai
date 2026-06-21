using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(EnglishAiDbContext))]
[Migration("20260727183000_RepairGrammarCuratedFormulasJson")]
public sealed class RepairGrammarCuratedFormulasJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "GrammarLessons"
            SET "CuratedFormulas" = '[]'::jsonb
            WHERE "CuratedFormulas" IS NULL
               OR jsonb_typeof("CuratedFormulas") <> 'array';
            """);

        migrationBuilder.AlterColumn<string>(
            name: "CuratedFormulas",
            table: "GrammarLessons",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]",
            oldClrType: typeof(string),
            oldType: "jsonb",
            oldDefaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "CuratedFormulas",
            table: "GrammarLessons",
            type: "jsonb",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "jsonb",
            oldDefaultValue: "[]");
    }
}
