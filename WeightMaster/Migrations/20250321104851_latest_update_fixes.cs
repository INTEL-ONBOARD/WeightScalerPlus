using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeightMaster.Migrations
{
    /// <inheritdoc />
    public partial class latest_update_fixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "RealValue",
                table: "transactionData",
                type: "float",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RealValue",
                table: "transactionData");
        }
    }
}
