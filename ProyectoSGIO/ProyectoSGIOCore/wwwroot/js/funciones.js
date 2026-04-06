// Función para mostrar/ocultar la contraseña
function togglePasswordVisibility(inputId, iconId) {
    const passwordInput = document.getElementById(inputId);
    const icon = document.getElementById(iconId);

    if (passwordInput.type === "password") {
        passwordInput.type = "text";
        icon.classList.remove("bi-eye-slash");
        icon.classList.add("bi-eye");
    } else {
        passwordInput.type = "password";
        icon.classList.remove("bi-eye");
        icon.classList.add("bi-eye-slash");
    }
}

// Función para agregar Fases a un Proyecto
function addPhase() {
    const phaseIndex = document.querySelectorAll('.fase-card').length;
    const phaseHtml = `
        <div class="fase-card" data-phase-index="${phaseIndex}">
            <div class="fase-header">
                <label>Ingrese un nombre para esta Fase</label>
                <input type="text" name="fases[${phaseIndex}].Nombre" placeholder="Nombre de la Fase" />
                
                <!-- Botón de eliminación de fase -->
                <button type="button" class="btn-delete-phase" onclick="deletePhase(${phaseIndex})">
                    <i class="fa fa-trash"></i>
                </button>
            </div>

            <hr />
            <h4>Tareas</h4>
            <div class="tareas-container" data-phase-index="${phaseIndex}"></div>

            <button type="button" class="btn btn-success mt-3" onclick="addTask(${phaseIndex})">Agregar Tarea</button>
        </div>
    `;
    document.getElementById('fases-container').insertAdjacentHTML('beforeend', phaseHtml);
    actualizarTotales(); // Actualizar totales al agregar una nueva fase
}

// Función para agregar Tareas a un Proyecto
function addTask(phaseIndex) {
    const tareasContainer = document.querySelector(`.tareas-container[data-phase-index="${phaseIndex}"]`);
    const taskIndex = tareasContainer.querySelectorAll('.tarea-item').length;
    const taskHtml = `
        <div class="tarea-item">
            <input type="text" name="fases[${phaseIndex}].Tareas[${taskIndex}].Nombre" placeholder="Nombre de la Tarea" />

            <label>Fecha de Inicio</label>
            <input type="date" name="fases[${phaseIndex}].Tareas[${taskIndex}].FechaInicio" />

            <label>Fecha de Fin</label>
            <input type="date" name="fases[${phaseIndex}].Tareas[${taskIndex}].FechaFin" />

            <label>Costo</label>
            <input type="number" step="1" min="0" name="fases[${phaseIndex}].Tareas[${taskIndex}].Costo" placeholder="Costo (opcional)" />

            <!-- Botón de eliminación -->
            <button type="button" class="btn-delete" onclick="eliminarTarea(this)">
                <i class="fa fa-trash"></i>
            </button>
        </div>
    `;
    tareasContainer.insertAdjacentHTML('beforeend', taskHtml);
    actualizarTotales(); // Actualizar totales al agregar una nueva tarea
}

// Función para agregar Hitos Proyecto
function addHito() {
    const phaseIndex = document.querySelectorAll('.fase-card').length;
    const phaseHtml = `
        <div class="fase-card" data-phase-index="${phaseIndex}">
            <div class="fase-header">
                <label>Ingrese una descripción para este Hito</label>
                <input type="text" name="hitos[${phaseIndex}].Descripcion" placeholder="Descripción del Hito" />
                
                <!-- Botón de eliminación del hito -->
                <button type="button" class="btn-delete-phase" onclick="deleteHito(${phaseIndex})">
                    <i class="fa fa-trash"></i>
                </button>
            </div>

           <div class="mb-3">
        <label class="form-label">Empleado</label>
        <select  name="hitos[${phaseIndex}].IdEmpleado" asp-items="ViewBag.Usuarios" class="form-select">
            <option value="">Seleccione un usuario</option>
        </select>
        <span class="text-danger"></span>
    </div>
             <div class="mb-3">
            <label>Fecha de Inicio</label>
            <input type="date" name="hitos[${phaseIndex}].Fecha" />
            </div>
            <hr />
        </div>
    `;
    document.getElementById('fases-container').insertAdjacentHTML('beforeend', phaseHtml);
    actualizarTotales(); // Actualizar totales al agregar una nueva fase
}

// Función para eliminar una Fase
function deletePhase(phaseIndex) {
    const phaseCard = document.querySelector(`.fase-card[data-phase-index="${phaseIndex}"]`);
    if (phaseCard) {
        phaseCard.remove();
        actualizarTotales(); // Actualizar totales al eliminar una fase
    }
}

// Función para eliminar una Tarea
function eliminarTarea(button) {
    const tareaItem = button.closest('.tarea-item');
    if (tareaItem) {
        tareaItem.remove();
        actualizarTotales(); // Actualizar totales al eliminar una tarea
    }
}

// Función para eliminar una Hito
function deleteHito(phaseIndex) {
    const phaseCard = document.querySelector(`.fase-card[data-phase-index="${phaseIndex}"]`);
    if (phaseCard) {
        phaseCard.remove();
    }
}

// ?
function obtenerNombreEstado(estado) {
    if (estado == 1) {
        return "Completo"
    }
    if (estado == 2) {
        return "Completo"
    }
    if (estado == 3) {
        return "En Proceso"
    }

    return ""
}

// Función para actualizar los totales de cada fase y del proyecto
function actualizarTotales() {
    let totalProyecto = 0;
    const totalesFases = document.getElementById('totalesFases');

    if (totalesFases) {
        totalesFases.innerHTML = '';

        document.querySelectorAll('.fase-card').forEach((fase, phaseIndex) => {
            let totalFase = 0;

            fase.querySelectorAll('input[name^="fases[' + phaseIndex + '].Tareas"]').forEach(tarea => {
                if (tarea.name.endsWith('.Costo')) {
                    const costo = parseFloat(tarea.value) || 0;
                    totalFase += costo;
                }
            });

            totalProyecto += totalFase;

            const faseTotalItem = document.createElement('li');
            faseTotalItem.textContent = `Fase ${phaseIndex + 1}: ${totalFase.toFixed(2)}`;
            totalesFases.appendChild(faseTotalItem);
        });

        document.getElementById('proyectoTotal').textContent = totalProyecto.toFixed(2);
    } else {
        console.warn('El elemento "totalesFases" no se encuentra en el DOM');
    }
}

document.addEventListener('input', function (event) {
    if (event.target.matches('input[name$=".Costo"]')) {
        actualizarTotales();
    }
});
