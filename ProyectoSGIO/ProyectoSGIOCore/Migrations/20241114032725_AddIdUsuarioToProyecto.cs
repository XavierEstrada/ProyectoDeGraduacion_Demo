using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoSGIOCore.Migrations
{
    /// <inheritdoc />
    public partial class AddIdUsuarioToProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioIdUsuario",
                table: "Proyectos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_UsuarioIdUsuario",
                table: "Proyectos",
                column: "UsuarioIdUsuario");

            migrationBuilder.AddForeignKey(
                name: "FK_Proyectos_Usuario_UsuarioIdUsuario",
                table: "Proyectos",
                column: "UsuarioIdUsuario",
                principalTable: "Usuario",
                principalColumn: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_Usuario_UsuarioIdUsuario",
                table: "Proyectos");

            migrationBuilder.DropIndex(
                name: "IX_Proyectos_UsuarioIdUsuario",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "UsuarioIdUsuario",
                table: "Proyectos");
        }
    }
}
