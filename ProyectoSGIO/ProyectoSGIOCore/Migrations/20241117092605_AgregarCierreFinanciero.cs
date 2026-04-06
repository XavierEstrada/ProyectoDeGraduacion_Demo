using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoSGIOCore.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCierreFinanciero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CierresFinancieros",
                columns: table => new
                {
                    IdCierre = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalIngresos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalEgresos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Utilidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierresFinancieros", x => x.IdCierre);
                });

           // migrationBuilder.CreateTable(
               // name: "Inventarios",
               // columns: table => new
              //  {
                 //   ID = table.Column<int>(type: "int", nullable: false)
                  //      .Annotation("SqlServer:Identity", "1, 1"),
                  //  Nombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                 //   Categoria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                //    Cantidad = table.Column<int>(type: "int", nullable: false),
                //    PrecioUnidad = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
               //     PrecioTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
               //     InformacionAdicional = table.Column<string>(type: "nvarchar(max)", nullable: true)
              //  },
             //   constraints: table =>
            //    {
           //         table.PrimaryKey("PK_Inventarios", x => x.ID);
           //     });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CierresFinancieros");

           // migrationBuilder.DropTable(
               // name: "Inventarios");
        }
    }
}
