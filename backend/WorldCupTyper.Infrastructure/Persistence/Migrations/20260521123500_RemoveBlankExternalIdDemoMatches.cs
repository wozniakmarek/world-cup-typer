using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupTyper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldCupTyperDbContext))]
    [Migration("20260521123500_RemoveBlankExternalIdDemoMatches")]
    public partial class RemoveBlankExternalIdDemoMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Matches" AS match
                WHERE match."ExternalId" IS NULL OR btrim(match."ExternalId") = '';

                DELETE FROM "Teams" AS team
                WHERE (team."ExternalId" IS NULL OR btrim(team."ExternalId") = '')
                    AND NOT EXISTS (
                        SELECT 1
                        FROM "Matches" AS match
                        WHERE match."HomeTeamId" = team."Id"
                            OR match."AwayTeamId" = team."Id"
                            OR match."WinnerTeamId" = team."Id"
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
