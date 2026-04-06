using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoSGIOCore.Models
{
    public class Hito
    {
        public int ID { get; set; }
        public int ProyectoId { get; set; }
        public Proyecto Proyecto { get; set; }

        [Required]
        public string Descripcion { get; set; }
        public int estado { get; set; }

        // Relación con Cliente
        public int? IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; }
    }
}
