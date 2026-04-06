using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoSGIOCore.Migrations
{
    /// <inheritdoc />
    public partial class TwoFA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TwoFA",
                table: "Usuario",
                type: "bit",
                maxLength: 1,
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Usuario",
                keyColumn: "IdUsuario",
                keyValue: 1,
                columns: new[] { "Clave", "TwoFA" },
                values: new object[] { "PTOmGpZfmyerAqbyAbnVHw==", false });

            migrationBuilder.UpdateData(
                table: "Usuario",
                keyColumn: "IdUsuario",
                keyValue: 2,
                columns: new[] { "Clave", "TwoFA" },
                values: new object[] { "PTOmGpZfmyerAqbyAbnVHw==", false });

            migrationBuilder.UpdateData(
                table: "Usuario",
                keyColumn: "IdUsuario",
                keyValue: 3,
                columns: new[] { "Clave", "TwoFA" },
                values: new object[] { "PTOmGpZfmyerAqbyAbnVHw==", false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwoFA",
                table: "Usuario");

            migrationBuilder.UpdateData(
                table: "Usuario",
                keyColumn: "IdUsuario",
                keyValue: 1,
                column: "Clave",
                value: "123");

            migrationBuilder.UpdateData(
                table: "Usuario",
                keyColumn: "IdUsuario",
                keyValue: 2,
                column: "Clave",
                value: "123");

            migrationBuilder.UpdateData(
                table: "Usuario",
                keyColumn: "IdUsuario",
                keyValue: 3,
                column: "Clave",
                value: "123");
        }
    }
}
