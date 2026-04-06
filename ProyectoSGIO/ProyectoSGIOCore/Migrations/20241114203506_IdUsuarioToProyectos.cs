using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoSGIOCore.Migrations
{
    /// <inheritdoc />
    public partial class IdUsuarioToProyectos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_Usuario_UsuarioIdUsuario",
                table: "Proyectos");

            migrationBuilder.RenameColumn(
                name: "UsuarioIdUsuario",
                table: "Proyectos",
                newName: "IdUsuario");

            migrationBuilder.RenameIndex(
                name: "IX_Proyectos_UsuarioIdUsuario",
                table: "Proyectos",
                newName: "IX_Proyectos_IdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Proyectos_Usuario_IdUsuario",
                table: "Proyectos",
                column: "IdUsuario",
                principalTable: "Usuario",
                principalColumn: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_Usuario_IdUsuario",
                table: "Proyectos");

            migrationBuilder.RenameColumn(
                name: "IdUsuario",
                table: "Proyectos",
                newName: "UsuarioIdUsuario");

            migrationBuilder.RenameIndex(
                name: "IX_Proyectos_IdUsuario",
                table: "Proyectos",
                newName: "IX_Proyectos_UsuarioIdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Proyectos_Usuario_UsuarioIdUsuario",
                table: "Proyectos",
                column: "UsuarioIdUsuario",
                principalTable: "Usuario",
                principalColumn: "IdUsuario");
        }
    }
}
