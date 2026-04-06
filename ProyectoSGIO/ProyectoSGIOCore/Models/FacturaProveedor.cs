using System.ComponentModel.DataAnnotations;

namespace ProyectoSGIOCore.Models
{
    public class FacturaProveedor
    {
        [Key]
        public int IdFactura { get; set; }

        [Required]
        public int IdProveedor { get; set; }

        [Required]
        public DateTime FechaEmision { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El monto debe ser positivo.")]
        public decimal MontoTotal { get; set; }

        public string NumeroFactura { get; set; }

        public string Descripcion { get; set; }

        // Relación con la entidad Proveedor
        public Proveedor Proveedor { get; set; }
    }
}
