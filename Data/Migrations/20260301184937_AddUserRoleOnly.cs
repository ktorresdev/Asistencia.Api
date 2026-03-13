using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asistencia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "USERS",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Employee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "USERS");
        }
    }
}
