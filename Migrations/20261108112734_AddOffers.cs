using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ARTISTO.Migrations
{
    /// <inheritdoc />
    public partial class AddOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArtworkListingId = table.Column<int>(type: "integer", nullable: false),
                    BuyerId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.CheckConstraint("CK_Offers_Amount", "CAST(\"Amount\" AS numeric) BETWEEN 0.01 AND 1000000");
                    table.ForeignKey(
                        name: "FK_Offers_ArtworkListings_ArtworkListingId",
                        column: x => x.ArtworkListingId,
                        principalTable: "ArtworkListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ArtworkListingId",
                table: "Offers",
                column: "ArtworkListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_BuyerId_ArtworkListingId",
                table: "Offers",
                columns: new[] { "BuyerId", "ArtworkListingId" },
                unique: true,
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Offers");
        }
    }
}
