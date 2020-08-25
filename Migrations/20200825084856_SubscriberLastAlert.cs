using Microsoft.EntityFrameworkCore.Migrations;

namespace Tuck.Migrations
{
    public partial class SubscriberLastAlert : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "LastAlert",
                table: "Subscriptions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAlert",
                table: "Subscriptions");
        }
    }
}
