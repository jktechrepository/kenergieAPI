using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    public partial class AjoutDateDerniereReactivationClient : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereReactivation",
                table: "Clients",
                type: "datetime(6)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateDerniereReactivation",
                table: "Clients");
        }
    }
}
