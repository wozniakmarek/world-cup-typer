using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupTyper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldCupTyperDbContext))]
    [Migration("20260521122000_RemoveRemainingDemoMatches")]
    public partial class RemoveRemainingDemoMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Matches" AS match
                WHERE match."ExternalId" IS NULL;

                DELETE FROM "Teams" AS team
                WHERE team."ExternalId" IS NULL
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
