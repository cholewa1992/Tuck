using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tuck.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buffs",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(nullable: false),
                    Username = table.Column<string>(nullable: true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Time = table.Column<DateTime>(nullable: false),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buffs", x => x.Id);
                });

            migrationBuilder.CreateIndex("IX_Buffs_DiscordId", "Buffs", "DiscordId");
            migrationBuilder.CreateIndex("IX_Buffs_UserId", "Buffs", "UserId");

            migrationBuilder.CreateTable(
                name: "Emotes",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emotes", x => x.Id);
                });

            migrationBuilder.CreateIndex("IX_Emotes_DiscordId", "Emotes", "DiscordId");
            migrationBuilder.CreateIndex("IX_Emotes_Name", "Emotes", "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Buffs");

            migrationBuilder.DropTable(
                name: "Emotes");
        }
    }
}
