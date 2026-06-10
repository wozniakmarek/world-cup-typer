using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupTyper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordChangeRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresPasswordChange",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresPasswordChange",
                table: "Users");
        }
    }
}
