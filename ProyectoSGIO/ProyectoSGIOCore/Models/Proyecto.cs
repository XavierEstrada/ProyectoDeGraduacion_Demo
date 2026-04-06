using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoSGIOCore.Models
{
    public class Proyecto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Relación con Fases
        public ICollection<Fase> Fases { get; set; } = new List<Fase>();
        public ICollection<Hito> Hitos { get; set; } = new List<Hito>();

        // Propiedad calculada para el costo total del proyecto
        public decimal CostoTotal => Fases?.Sum(f => f.CostoTotal) ?? 0;

        // Relación con Cliente
        public int? IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; }

        public EstadoProyecto Estado { get; set; } //Estado del proyecto
    }

    public enum EstadoProyecto
    {
        Planificacion = 1,
        Progreso = 2,
        Completado = 3,
        Pendiente = 4
    }
}