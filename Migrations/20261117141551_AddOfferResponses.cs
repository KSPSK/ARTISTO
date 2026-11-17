using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARTISTO.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CounterAmount",
                table: "Offers",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CounterMessage",
                table: "Offers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "Offers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAtUtc",
                table: "Offers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OrderId",
                table: "Offers",
                column: "OrderId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Offers_CounterAmount",
                table: "Offers",
                sql: "\"CounterAmount\" IS NULL OR CAST(\"CounterAmount\" AS numeric) BETWEEN 0.01 AND 1000000");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Orders_OrderId",
                table: "Offers",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Offers_Orders_OrderId",
                table: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_Offers_OrderId",
                table: "Offers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Offers_CounterAmount",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "CounterAmount",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "CounterMessage",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "RespondedAtUtc",
                table: "Offers");
        }
    }
}
