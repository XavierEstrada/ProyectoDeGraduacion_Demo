namespace ProyectoSGIOCore.Models
{
    public class RegistroActividad
    {
        public int Id { get; set; }
        public string UsuarioNombre { get; set; }
        public string Accion { get; set; } // "creó", "editó", "eliminó"
        public string Entidad { get; set; } // "Proyecto", "Fase", "Tarea", "Hito", "Factura", "Cliente"...
        public string Detalle { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
