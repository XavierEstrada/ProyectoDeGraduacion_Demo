namespace ProyectoSGIOCore.Models
{
    public class Adjunto
    {
        public int Id { get; set; }
        public string EntidadTipo { get; set; } // "Factura", "Tarea", "Hito"...
        public int EntidadId { get; set; }
        public string NombreArchivo { get; set; }
        public string RutaStorage { get; set; }
        public string UrlPublica { get; set; }
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
    }
}
