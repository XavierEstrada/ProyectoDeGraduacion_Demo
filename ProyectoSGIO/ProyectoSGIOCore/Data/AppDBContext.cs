using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Models;

namespace ProyectoSGIOCore.Data
{
    public class AppDBContext : DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options)
            : base(options)
        {

        }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Inventario> Inventarios { get; set; }
        public DbSet<FacturaProveedor> Facturas { get; set; }
        public DbSet<Proyecto> Proyectos { get; set; }
        public DbSet<Fase> Fases { get; set; }
        public DbSet<Tarea> Tareas { get; set; }
        public DbSet<Hito> Hitos { get; set; }
        public DbSet<CierreFinanciero> CierresFinancieros { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Usuario
            modelBuilder.Entity<Usuario>(tb =>
            {
                tb.HasKey(u => u.IdUsuario);
                tb.Property(u => u.IdUsuario)
                  .ValueGeneratedOnAdd();

                tb.Property(u => u.Nombre).HasMaxLength(50).IsRequired();
                tb.Property(u => u.Apellido).HasMaxLength(50).IsRequired();
                tb.Property(u => u.Correo).HasMaxLength(50).IsRequired();
                tb.Property(u => u.Clave).HasMaxLength(50).IsRequired();
                tb.Property(u => u.Activo).HasMaxLength(1).IsRequired();
                tb.Property(u => u.Temporal).HasMaxLength(1).IsRequired();
                tb.Property(u => u.TwoFA).HasMaxLength(1).IsRequired();
                tb.Property(u => u.intentos).HasMaxLength(10).IsRequired();

                tb.HasOne(u => u.Rol)
                  .WithMany(r => r.Usuarios)
                  .HasForeignKey(u => u.IdRol);
            });

            // Configuración de Rol
            modelBuilder.Entity<Rol>(tb =>
            {
                tb.HasKey(r => r.IdRol);
                tb.Property(r => r.IdRol)
                  .ValueGeneratedOnAdd();

                tb.Property(r => r.Nombre).HasMaxLength(50).IsRequired();

                tb.HasMany(r => r.Usuarios)
                  .WithOne(u => u.Rol)
                  .HasForeignKey(u => u.IdRol);
            });

            // Configuración de Proveedor
            modelBuilder.Entity<Proveedor>(tb =>
            {
                tb.HasKey(p => p.IdProveedor);
                tb.Property(p => p.IdProveedor)
                  .ValueGeneratedOnAdd();

                tb.Property(p => p.Nombre).HasMaxLength(50).IsRequired();
                tb.Property(p => p.Correo).HasMaxLength(50).IsRequired();
                tb.Property(p => p.Telefono).HasMaxLength(15).IsRequired();
                tb.Property(p => p.Direccion).HasMaxLength(200).IsRequired();
                tb.Property(p => p.Estado).IsRequired();
            });

            // Configuración de Usuario
            modelBuilder.Entity<Empleado>(tb =>
            {
                tb.HasKey(e => e.IdEmpleado);
                tb.Property(e => e.IdEmpleado)
                  .ValueGeneratedOnAdd();

                tb.Property(u => u.Nombre).HasMaxLength(50).IsRequired();
                tb.Property(u => u.Apellido).HasMaxLength(50).IsRequired();
                tb.Property(u => u.Correo).HasMaxLength(50).IsRequired();

            });
            
            // Tablas
            modelBuilder.Entity<Usuario>().ToTable("Usuario");
            modelBuilder.Entity<Rol>().ToTable("Rol");
            modelBuilder.Entity<Proveedor>().ToTable("Proveedor");
            modelBuilder.Entity<Empleado>().ToTable("Empleado");

            modelBuilder.Entity<Rol>().HasData(
               new Rol { IdRol = 1, Nombre = "Administrador" },
               new Rol { IdRol = 2, Nombre = "Supervisor" },
               new Rol { IdRol = 3, Nombre = "Empleado" },
               new Rol { IdRol = 4, Nombre = "Usuario" }
               );

            modelBuilder.Entity<Usuario>().HasData(
               new Usuario { IdUsuario = 1, Nombre = "Administador", Apellido = "User", Correo = "cpingenieria.sgio@gmail.com", Clave = "P#57?fFXzNhS5o", IdRol = 1, Activo = true, Temporal = false, TwoFA = false, intentos = 0 }
               );
        }
    }
}