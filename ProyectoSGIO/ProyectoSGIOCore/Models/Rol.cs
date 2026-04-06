namespace ProyectoSGIOCore.Models
{
    public class Rol
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; }

        // Relación con usuarios
        public ICollection<Usuario> Usuarios { get; set; }

    }
}
