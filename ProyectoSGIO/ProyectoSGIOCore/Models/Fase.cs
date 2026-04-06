namespace ProyectoSGIOCore.Models
{
    public class Fase
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int ProyectoId { get; set; }
        public Proyecto Proyecto { get; set; }
        public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();

        // Propiedad calculada para el costo total de la fase
        public decimal CostoTotal => Tareas?.Sum(t => t.Costo ?? 0) ?? 0;
    }
}
