using Microsoft.AspNetCore.Mvc;

namespace ProyectoSGIOCore.Models
{
    public class Proveedor
    {
        public int IdProveedor { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public bool Estado { get; set; } // true: Activo, false: Inactivo
    }
}
