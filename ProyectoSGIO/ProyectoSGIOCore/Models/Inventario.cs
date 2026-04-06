using System.ComponentModel.DataAnnotations;

namespace ProyectoSGIOCore.Models
{
    public class Inventario
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        public string Nombre { get; set; }
        
        [Required(ErrorMessage = "La categoría es obligatoria.")]
        public string Categoria { get; set; }

        [Required(ErrorMessage = "La Cantidad es obligatoria.")]
        public int Cantidad { get; set; }
        
        [Required(ErrorMessage = "El precio es obligatorio.")]
        public decimal PrecioUnidad { get; set; }
        public decimal PrecioTotal { get; set; }
        public bool Stock => Cantidad > 0;
        public string? InformacionAdicional { get; set; }
    }

}
