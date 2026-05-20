using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupTyper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFootballDataExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Teams" ADD COLUMN IF NOT EXISTS "ExternalId" character varying(100);

                DROP INDEX IF EXISTS "IX_Teams_ExternalId";
                CREATE UNIQUE INDEX "IX_Teams_ExternalId"
                    ON "Teams" ("ExternalId")
                    WHERE "ExternalId" IS NOT NULL AND btrim("ExternalId") <> '';

                DROP INDEX IF EXISTS "IX_Matches_ExternalId";
                CREATE UNIQUE INDEX "IX_Matches_ExternalId"
                    ON "Matches" ("ExternalId")
                    WHERE "ExternalId" IS NOT NULL AND btrim("ExternalId") <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Teams_ExternalId";
                DROP INDEX IF EXISTS "IX_Matches_ExternalId";
                ALTER TABLE "Teams" DROP COLUMN IF EXISTS "ExternalId";
                """);
        }
    }
}
