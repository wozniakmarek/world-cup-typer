using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupTyper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldCupTyperDbContext))]
    [Migration("20260521084500_RemoveLegacySeededMatches")]
    public partial class RemoveLegacySeededMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Matches" AS match
                WHERE match."ExternalId" IS NULL
                    AND match."MatchNumber" IN (1, 2, 3, 4, 5);

                DELETE FROM "Teams" AS team
                WHERE team."ExternalId" IS NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM "Matches" AS match
                        WHERE match."HomeTeamId" = team."Id"
                            OR match."AwayTeamId" = team."Id"
                            OR match."WinnerTeamId" = team."Id"
                    )
                    AND (
                        lower(team."Name") IN (
                            'polska',
                            'poland',
                            'niemcy',
                            'germany',
                            'francja',
                            'france',
                            'hiszpania',
                            'spain',
                            'brazylia',
                            'brazil',
                            'argentyna',
                            'argentina',
                            'anglia',
                            'england',
                            'portugalia',
                            'portugal'
                        )
                        OR upper(team."ShortName") IN ('POL', 'GER', 'FRA', 'ESP', 'BRA', 'ARG', 'ENG', 'POR')
                        OR upper(team."CountryCode") IN ('PL', 'POL', 'DE', 'GER', 'FR', 'FRA', 'ES', 'ESP', 'BR', 'BRA', 'AR', 'ARG', 'GB', 'ENG', 'PT', 'POR')
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
