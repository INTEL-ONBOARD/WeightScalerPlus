using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeightMaster.Config;

#nullable disable

namespace WeightMaster.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260528121000_AddLineIdToGreenLeafPosts")]
    public partial class AddLineIdToGreenLeafPosts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "line_id",
                table: "GreenLeafPosts",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "line_id",
                table: "GreenLeafPosts");
        }
    }
}
