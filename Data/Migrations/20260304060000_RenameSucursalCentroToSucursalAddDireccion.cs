using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asistencia.Data.Migrations
{
    public partial class RenameSucursalCentroToSucursalAddDireccion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SUCURSAL_CENTRO",
                newName: "SUCURSAL");

            migrationBuilder.AddColumn<string>(
                name: "direccion",
                table: "SUCURSAL",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "direccion",
                table: "SUCURSAL");

            migrationBuilder.RenameTable(
                name: "SUCURSAL",
                newName: "SUCURSAL_CENTRO");
        }
    }
}
