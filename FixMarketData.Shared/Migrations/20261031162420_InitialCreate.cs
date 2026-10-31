using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixMarketData.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketDataTicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BidPrice = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    AskPrice = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    BidSize = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    AskSize = table.Column<decimal>(type: "decimal(18,5)", precision: 18, scale: 5, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketDataTicks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketDataTicks");
        }
    }
}
