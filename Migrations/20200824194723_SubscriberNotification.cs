using Microsoft.EntityFrameworkCore.Migrations;

namespace Tuck.Migrations
{
    public partial class SubscriberNotification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "SubscriberAlert",
                table: "Subscriptions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriberAlert",
                table: "Subscriptions");
        }
    }
}
