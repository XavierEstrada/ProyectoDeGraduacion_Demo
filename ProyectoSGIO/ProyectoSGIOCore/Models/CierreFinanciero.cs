using System.ComponentModel.DataAnnotations;

namespace ProyectoSGIOCore.Models
{
    public class CierreFinanciero
    {
        [Key]
        public int IdCierre { get; set; }

        [Required]
        public int Anio { get; set; } // Año del cierre

        public int Mes { get; set; } // Mes del cierre

        [Required]
        public DateTime FechaCierre { get; set; } = DateTime.Now;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalIngresos { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalEgresos { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Utilidad { get; set; } // TotalIngresos - TotalEgresos

        public string Observaciones { get; set; }

        public string TipoCierre { get; set; }
    }
}
