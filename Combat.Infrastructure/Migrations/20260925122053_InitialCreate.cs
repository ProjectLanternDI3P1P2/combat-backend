using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Combat.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Monsters",
            columns: table => new
            {
                MonsterId = table.Column<Guid>(type: "uuid", nullable: false),
                MonsterCombatId = table.Column<Guid>(type: "uuid", nullable: false),
                MonsterIsBoss = table.Column<bool>(type: "boolean", nullable: false),
                MonsterBaseHp = table.Column<int>(type: "integer", nullable: false),
                MonsterBaseAttack = table.Column<int>(type: "integer", nullable: false),
                MonsterBaseDefense = table.Column<int>(type: "integer", nullable: false),
                MonsterBaseSpeed = table.Column<int>(type: "integer", nullable: false),
                MonsterState = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Monsters", x => x.MonsterId);
            });

        migrationBuilder.CreateTable(
            name: "Players",
            columns: table => new
            {
                PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                PlayerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PlayerHealth = table.Column<int>(type: "integer", nullable: false),
                PlayerMaxHealth = table.Column<int>(type: "integer", nullable: false),
                PlayerAttack = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Players", x => x.PlayerId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Monsters");

        migrationBuilder.DropTable(
            name: "Players");
    }
}
