using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Combat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCombatEngineState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CombatActiveFighterId",
                table: "CombatInstances",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CombatEndedAt",
                table: "CombatInstances",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "CombatEvents",
                table: "CombatInstances",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]"
            );

            migrationBuilder.AddColumn<int>(
                name: "CombatFloor",
                table: "CombatInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<string>(
                name: "CombatProcessedCommands",
                table: "CombatInstances",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}"
            );

            migrationBuilder.AddColumn<string>(
                name: "CombatResult",
                table: "CombatInstances",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CombatResultReportedAt",
                table: "CombatInstances",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "CombatRoomId",
                table: "CombatInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "CombatRound",
                table: "CombatInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "CombatRoundIndex",
                table: "CombatInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<string>(
                name: "CombatRoundOrder",
                table: "CombatInstances",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "CombatRunId",
                table: "CombatInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<int>(
                name: "CombatTurn",
                table: "CombatInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CombatTurnDeadline",
                table: "CombatInstances",
                type: "timestamp with time zone",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "CombatTurnDurationSeconds",
                table: "CombatInstances",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CombatInstances",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u
            );

            migrationBuilder.AddColumn<bool>(
                name: "CombatFighterIsDefending",
                table: "CombatFighters",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "CombatFighterOrder",
                table: "CombatFighters",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<string>(
                name: "CombatFighterVisual",
                table: "CombatFighters",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}"
            );

            // Balance of the seeded monster definitions against the Player hero formulas.
            migrationBuilder.Sql(
                """
                UPDATE "MonsterDefinitions" SET "MonsterDefinitionBaseHealth" = 22, "MonsterDefinitionBaseAttack" = 12, "MonsterDefinitionBaseDefense" = 6, "MonsterDefinitionBaseSpeed" = 9 WHERE "MonsterDefinitionId" = '0d89a169-cfa8-4c0f-8e27-59e1cedbb201';
                UPDATE "MonsterDefinitions" SET "MonsterDefinitionBaseHealth" = 18, "MonsterDefinitionBaseAttack" = 13, "MonsterDefinitionBaseDefense" = 3, "MonsterDefinitionBaseSpeed" = 13 WHERE "MonsterDefinitionId" = '0d89a169-cfa8-4c0f-8e27-59e1cedbb202';
                UPDATE "MonsterDefinitions" SET "MonsterDefinitionBaseHealth" = 26, "MonsterDefinitionBaseAttack" = 14, "MonsterDefinitionBaseDefense" = 5, "MonsterDefinitionBaseSpeed" = 6 WHERE "MonsterDefinitionId" = '0d89a169-cfa8-4c0f-8e27-59e1cedbb203';
                UPDATE "MonsterDefinitions" SET "MonsterDefinitionBaseHealth" = 130, "MonsterDefinitionBaseAttack" = 22, "MonsterDefinitionBaseDefense" = 10, "MonsterDefinitionBaseSpeed" = 6 WHERE "MonsterDefinitionId" = '0d89a169-cfa8-4c0f-8e27-59e1cedbb207';
                UPDATE "MonsterDefinitions" SET "MonsterDefinitionBaseHealth" = 110, "MonsterDefinitionBaseAttack" = 24, "MonsterDefinitionBaseDefense" = 8, "MonsterDefinitionBaseSpeed" = 9 WHERE "MonsterDefinitionId" = '0d89a169-cfa8-4c0f-8e27-59e1cedbb208';
                UPDATE "MonsterDefinitions" SET "MonsterDefinitionBaseHealth" = 120, "MonsterDefinitionBaseAttack" = 20, "MonsterDefinitionBaseDefense" = 12, "MonsterDefinitionBaseSpeed" = 8 WHERE "MonsterDefinitionId" = '0d89a169-cfa8-4c0f-8e27-59e1cedbb209';
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_CombatInstances_CombatEndedAt",
                table: "CombatInstances",
                column: "CombatEndedAt",
                filter: "\"CombatResultReportedAt\" IS NULL AND \"CombatEndedAt\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CombatInstances_CombatRunId",
                table: "CombatInstances",
                column: "CombatRunId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CombatInstances_CombatEndedAt",
                table: "CombatInstances"
            );

            migrationBuilder.DropIndex(
                name: "IX_CombatInstances_CombatRunId",
                table: "CombatInstances"
            );

            migrationBuilder.DropColumn(name: "CombatActiveFighterId", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatEndedAt", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatEvents", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatFloor", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatProcessedCommands", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatResult", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatResultReportedAt", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatRoomId", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatRound", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatRoundIndex", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatRoundOrder", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatRunId", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatTurn", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatTurnDeadline", table: "CombatInstances");

            migrationBuilder.DropColumn(
                name: "CombatTurnDurationSeconds",
                table: "CombatInstances"
            );

            migrationBuilder.DropColumn(name: "xmin", table: "CombatInstances");

            migrationBuilder.DropColumn(name: "CombatFighterIsDefending", table: "CombatFighters");

            migrationBuilder.DropColumn(name: "CombatFighterOrder", table: "CombatFighters");

            migrationBuilder.DropColumn(name: "CombatFighterVisual", table: "CombatFighters");
        }
    }
}
