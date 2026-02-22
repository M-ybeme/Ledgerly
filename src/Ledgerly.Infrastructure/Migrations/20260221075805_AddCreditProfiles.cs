using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledgerly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentScoreRangeLow = table.Column<int>(type: "integer", nullable: false),
                    CurrentScoreRangeHigh = table.Column<int>(type: "integer", nullable: false),
                    PaymentHistoryIsClean = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditProfiles_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditAccountProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebtAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    AgeMonths = table.Column<int>(type: "integer", nullable: false),
                    AccountType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAccountProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditAccountProfiles_CreditProfiles_CreditProfileId",
                        column: x => x.CreditProfileId,
                        principalTable: "CreditProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditAccountProfiles_DebtAccounts_DebtAccountId",
                        column: x => x.DebtAccountId,
                        principalTable: "DebtAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccountProfiles_CreditProfileId",
                table: "CreditAccountProfiles",
                column: "CreditProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccountProfiles_DebtAccountId",
                table: "CreditAccountProfiles",
                column: "DebtAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditProfiles_ScenarioId",
                table: "CreditProfiles",
                column: "ScenarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditAccountProfiles");

            migrationBuilder.DropTable(
                name: "CreditProfiles");
        }
    }
}
