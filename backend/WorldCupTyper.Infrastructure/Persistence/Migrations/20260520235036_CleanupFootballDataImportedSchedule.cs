using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupTyper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CleanupFootballDataImportedSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Matches" AS match
                WHERE match."ExternalId" LIKE 'football-data:%'
                    AND NOT EXISTS (
                        SELECT 1
                        FROM "Predictions" AS prediction
                        WHERE prediction."MatchId" = match."Id"
                    );

                DELETE FROM "Teams" AS team
                WHERE NOT EXISTS (
                        SELECT 1
                        FROM "Matches" AS match
                        WHERE match."HomeTeamId" = team."Id"
                            OR match."AwayTeamId" = team."Id"
                            OR match."WinnerTeamId" = team."Id"
                    )
                    AND (
                        team."ExternalId" LIKE 'football-data:%'
                        OR (
                            team."ExternalId" IS NULL
                            AND (
                                lower(team."Name") IN ('to be announced', 'tba', 'tbd', 'unknown team')
                                OR lower(team."ShortName") IN ('tba', 'tbd')
                                OR team."Name" LIKE 'Winner Group %'
                                OR team."Name" LIKE 'Runner-up Group %'
                                OR team."ShortName" LIKE 'Winner Group %'
                                OR team."ShortName" LIKE 'Runner-up Group %'
                            )
                        )
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
