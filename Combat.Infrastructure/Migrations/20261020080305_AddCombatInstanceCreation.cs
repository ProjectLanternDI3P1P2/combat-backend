using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Combat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatInstanceCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CombatInstances",
                columns: table => new
                {
                    CombatId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatEncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatCommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatEncounterType = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    CombatStatus = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    CombatStartedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatInstances", x => x.CombatId);
                }
            );

            migrationBuilder.CreateTable(
                name: "CombatFighters",
                columns: table => new
                {
                    CombatFighterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatFighterType = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    CombatFighterExternalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombatFighterName = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    CombatFighterLevel = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterIsFromMockedData = table.Column<bool>(
                        type: "boolean",
                        nullable: false
                    ),
                    CombatFighterCurrentHp = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterMaxHp = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterCurrentMana = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterMaxMana = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterAttack = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterDefense = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterSpeed = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterState = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    CombatFighterInitialCurrentHp = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    CombatFighterInitialMaxHp = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterInitialCurrentMana = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    CombatFighterInitialMaxMana = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    CombatFighterInitialAttack = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    CombatFighterInitialDefense = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    CombatFighterInitialSpeed = table.Column<int>(type: "integer", nullable: false),
                    CombatFighterAbilities = table.Column<string>(type: "jsonb", nullable: false),
                    CombatFighterItems = table.Column<string>(type: "jsonb", nullable: false),
                    CombatFighterEffects = table.Column<string>(type: "jsonb", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatFighters", x => x.CombatFighterId);
                    table.ForeignKey(
                        name: "FK_CombatFighters_CombatInstances_CombatId",
                        column: x => x.CombatId,
                        principalTable: "CombatInstances",
                        principalColumn: "CombatId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CombatFighters_CombatId",
                table: "CombatFighters",
                column: "CombatId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CombatInstances_CombatCommandId",
                table: "CombatInstances",
                column: "CombatCommandId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CombatInstances_CombatEncounterId",
                table: "CombatInstances",
                column: "CombatEncounterId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_CombatInstances_CombatSessionId",
                table: "CombatInstances",
                column: "CombatSessionId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CombatFighters");

            migrationBuilder.DropTable(name: "CombatInstances");
        }
    }
}
