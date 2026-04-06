using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProyectoSGIOCore.Models
{
    public class Tarea
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Nombre { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Completada { get; set; } = false;
        public decimal? Costo { get; set; }

        // Relación con el Proyecto
        public int FaseId { get; set; }
        public Fase Fase { get; set; }
    }
}