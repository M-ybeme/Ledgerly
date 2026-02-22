using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledgerly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtsAndScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DebtAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualInterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumPayment = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ExtraMonthlyPayment = table.Column<decimal>(type: "numeric", nullable: false),
                    Strategy = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioDebtAccounts",
                columns: table => new
                {
                    DebtAccountsId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenariosId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioDebtAccounts", x => new { x.DebtAccountsId, x.ScenariosId });
                    table.ForeignKey(
                        name: "FK_ScenarioDebtAccounts_DebtAccounts_DebtAccountsId",
                        column: x => x.DebtAccountsId,
                        principalTable: "DebtAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScenarioDebtAccounts_Scenarios_ScenariosId",
                        column: x => x.ScenariosId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioDebtAccounts_ScenariosId",
                table: "ScenarioDebtAccounts",
                column: "ScenariosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScenarioDebtAccounts");

            migrationBuilder.DropTable(
                name: "DebtAccounts");

            migrationBuilder.DropTable(
                name: "Scenarios");
        }
    }
}
