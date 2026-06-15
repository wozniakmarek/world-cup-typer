using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupTyper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPushSubscriptionDeviceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "PushSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId_DeviceId",
                table: "PushSubscriptions",
                columns: new[] { "UserId", "DeviceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PushSubscriptions_UserId_DeviceId",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "PushSubscriptions");
        }
    }
}
