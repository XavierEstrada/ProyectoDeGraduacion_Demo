using ProyectoSGIOCore.Models;
using Xunit;

namespace ProyectoSGIOCore.Tests
{
    public class CostoTotalTests
    {
        private static Tarea NuevaTarea(decimal? costo) => new Tarea
        {
            Nombre = "Tarea",
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddDays(1),
            Costo = costo
        };

        [Fact]
        public void Fase_CostoTotal_SumsTareaCostos()
        {
            var fase = new Fase
            {
                Nombre = "Fase",
                Tareas = new List<Tarea> { NuevaTarea(100.50m), NuevaTarea(49.50m) }
            };

            Assert.Equal(150.00m, fase.CostoTotal);
        }

        [Fact]
        public void Fase_CostoTotal_TreatsNullCostoAsZero()
        {
            var fase = new Fase
            {
                Nombre = "Fase",
                Tareas = new List<Tarea> { NuevaTarea(100m), NuevaTarea(null) }
            };

            Assert.Equal(100m, fase.CostoTotal);
        }

        [Fact]
        public void Fase_CostoTotal_EmptyTareas_ReturnsZero()
        {
            var fase = new Fase { Nombre = "Fase", Tareas = new List<Tarea>() };

            Assert.Equal(0m, fase.CostoTotal);
        }

        [Fact]
        public void Proyecto_CostoTotal_SumsAllFaseCostos()
        {
            var proyecto = new Proyecto
            {
                Nombre = "Proyecto",
                Fases = new List<Fase>
                {
                    new Fase { Nombre = "Fase 1", Tareas = new List<Tarea> { NuevaTarea(1000m) } },
                    new Fase { Nombre = "Fase 2", Tareas = new List<Tarea> { NuevaTarea(500m), NuevaTarea(250m) } }
                }
            };

            Assert.Equal(1750m, proyecto.CostoTotal);
        }

        [Fact]
        public void Proyecto_CostoTotal_NoFases_ReturnsZero()
        {
            var proyecto = new Proyecto { Nombre = "Proyecto", Fases = new List<Fase>() };

            Assert.Equal(0m, proyecto.CostoTotal);
        }
    }
}
