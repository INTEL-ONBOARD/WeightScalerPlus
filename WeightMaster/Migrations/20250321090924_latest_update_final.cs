using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeightMaster.Migrations
{
    /// <inheritdoc />
    public partial class latest_update_final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinaltransactionData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LineName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TransportAgent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Company = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LeafWeightOfficer = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Supervisor = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BarcodeDetails = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameWithInitials = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    BagCount = table.Column<int>(type: "int", nullable: false),
                    MaximumNormalLeafWeight = table.Column<float>(type: "float", nullable: false),
                    TotalLeafWeight = table.Column<float>(type: "float", nullable: false),
                    ActualNormalLeafWeight = table.Column<float>(type: "float", nullable: false),
                    TotalGoldLeafWeight = table.Column<float>(type: "float", nullable: false),
                    Water = table.Column<float>(type: "float", nullable: false),
                    Morapuwata = table.Column<float>(type: "float", nullable: false),
                    Thambimata = table.Column<float>(type: "float", nullable: false),
                    Reject = table.Column<float>(type: "float", nullable: false),
                    BagWeight = table.Column<float>(type: "float", nullable: false),
                    FinalGreenLeafCount = table.Column<int>(type: "int", nullable: false),
                    FinalGoldLeafCount = table.Column<int>(type: "int", nullable: false),
                    RealValue = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinaltransactionData", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinaltransactionData");
        }
    }
}
