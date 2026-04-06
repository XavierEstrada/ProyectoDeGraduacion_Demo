using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoSGIOCore.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoCierreFinanciero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MargenUtilidad",
                table: "CierresFinancieros");

            migrationBuilder.DropColumn(
                name: "TotalImpuestos",
                table: "CierresFinancieros");

            migrationBuilder.AddColumn<string>(
                name: "TipoCierre",
                table: "CierresFinancieros",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoCierre",
                table: "CierresFinancieros");

            migrationBuilder.AddColumn<decimal>(
                name: "MargenUtilidad",
                table: "CierresFinancieros",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalImpuestos",
                table: "CierresFinancieros",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
