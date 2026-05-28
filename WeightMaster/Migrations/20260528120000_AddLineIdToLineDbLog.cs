using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WeightMaster.Config;

#nullable disable

namespace WeightMaster.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260528120000_AddLineIdToLineDbLog")]
    public partial class AddLineIdToLineDbLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "lineid",
                table: "lineDbLog",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lineid",
                table: "lineDbLog");
        }
    }
}
