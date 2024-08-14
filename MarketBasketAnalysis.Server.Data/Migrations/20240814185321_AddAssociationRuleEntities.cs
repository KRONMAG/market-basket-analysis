using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketBasketAnalysis.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssociationRuleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssociationRuleSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    TransactionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLoaded = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationRuleSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssociationRuleChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PayloadSize = table.Column<int>(type: "INTEGER", nullable: false),
                    AssociationRuleSetId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationRuleChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationRuleChunks_AssociationRuleSets_AssociationRuleSetId",
                        column: x => x.AssociationRuleSetId,
                        principalTable: "AssociationRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PayloadSize = table.Column<int>(type: "INTEGER", nullable: false),
                    AssociationRuleSetId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemChunks_AssociationRuleSets_AssociationRuleSetId",
                        column: x => x.AssociationRuleSetId,
                        principalTable: "AssociationRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationRuleChunks_AssociationRuleSetId",
                table: "AssociationRuleChunks",
                column: "AssociationRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationRuleSets_Name",
                table: "AssociationRuleSets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemChunks_AssociationRuleSetId",
                table: "ItemChunks",
                column: "AssociationRuleSetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssociationRuleChunks");

            migrationBuilder.DropTable(
                name: "ItemChunks");

            migrationBuilder.DropTable(
                name: "AssociationRuleSets");
        }
    }
}
