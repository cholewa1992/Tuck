using Microsoft.EntityFrameworkCore.Migrations;

namespace Tuck.Migrations
{
    public partial class NinjaPop : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ninja",
                table: "Buffs",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ninja",
                table: "Buffs");
        }
    }
}
