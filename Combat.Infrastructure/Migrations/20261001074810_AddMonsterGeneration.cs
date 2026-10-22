using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Combat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonsterDefinitions",
                columns: table => new
                {
                    MonsterDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonsterDefinitionName = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    MonsterDefinitionClass = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    MonsterDefinitionImageUrl = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    MonsterDefinitionBaseHealth = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    MonsterDefinitionBaseAttack = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    MonsterDefinitionBaseDefense = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    MonsterDefinitionBaseSpeed = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonsterDefinitions", x => x.MonsterDefinitionId);
                }
            );

            migrationBuilder.CreateTable(
                name: "Monsters",
                columns: table => new
                {
                    MonsterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonsterDungeonRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonsterIdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    MonsterDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonsterName = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    MonsterClass = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    MonsterLevel = table.Column<int>(type: "integer", nullable: false),
                    MonsterPlayerCount = table.Column<int>(type: "integer", nullable: false),
                    MonsterImageUrl = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    MonsterBaseHealth = table.Column<int>(type: "integer", nullable: false),
                    MonsterBaseAttack = table.Column<int>(type: "integer", nullable: false),
                    MonsterBaseDefense = table.Column<int>(type: "integer", nullable: false),
                    MonsterBaseSpeed = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monsters", x => x.MonsterId);
                    table.ForeignKey(
                        name: "FK_Monsters_MonsterDefinitions_MonsterDefinitionId",
                        column: x => x.MonsterDefinitionId,
                        principalTable: "MonsterDefinitions",
                        principalColumn: "MonsterDefinitionId",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MonsterDefinitions_MonsterDefinitionClass",
                table: "MonsterDefinitions",
                column: "MonsterDefinitionClass"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Monsters_MonsterDefinitionId",
                table: "Monsters",
                column: "MonsterDefinitionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Monsters_MonsterIdempotencyKey",
                table: "Monsters",
                column: "MonsterIdempotencyKey",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Monsters");

            migrationBuilder.DropTable(name: "MonsterDefinitions");
        }
    }
}
