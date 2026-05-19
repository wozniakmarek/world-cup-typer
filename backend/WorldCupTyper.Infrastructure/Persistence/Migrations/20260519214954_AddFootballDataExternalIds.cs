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
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Teams",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ExternalId",
                table: "Teams",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ExternalId",
                table: "Matches",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_ExternalId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Matches_ExternalId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Teams");
        }
    }
}
