using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoSGIOCore.Migrations
{
    /// <inheritdoc />
    public partial class FasesProyectos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tareas_Proyectos_ProyectoId",
                table: "Tareas");

            migrationBuilder.RenameColumn(
                name: "ProyectoId",
                table: "Tareas",
                newName: "FaseId");

            migrationBuilder.RenameIndex(
                name: "IX_Tareas_ProyectoId",
                table: "Tareas",
                newName: "IX_Tareas_FaseId");

            migrationBuilder.CreateTable(
                name: "Fases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProyectoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fases_Proyectos_ProyectoId",
                        column: x => x.ProyectoId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fases_ProyectoId",
                table: "Fases",
                column: "ProyectoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tareas_Fases_FaseId",
                table: "Tareas",
                column: "FaseId",
                principalTable: "Fases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tareas_Fases_FaseId",
                table: "Tareas");

            migrationBuilder.DropTable(
                name: "Fases");

            migrationBuilder.RenameColumn(
                name: "FaseId",
                table: "Tareas",
                newName: "ProyectoId");

            migrationBuilder.RenameIndex(
                name: "IX_Tareas_FaseId",
                table: "Tareas",
                newName: "IX_Tareas_ProyectoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tareas_Proyectos_ProyectoId",
                table: "Tareas",
                column: "ProyectoId",
                principalTable: "Proyectos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
