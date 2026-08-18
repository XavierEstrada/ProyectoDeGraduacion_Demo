namespace ProyectoSGIOCore.Models
{
    public class Comentario
    {
        public int Id { get; set; }
        public string EntidadTipo { get; set; } // "Tarea", "Hito"
        public int EntidadId { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; }
        public string Texto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
