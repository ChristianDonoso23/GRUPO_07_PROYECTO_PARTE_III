// =========================================================
// SCRIPT PARA SELECCIÓN DE DÍAS EN ESPECIALIDADES
// =========================================================

/**
 * Inicializar eventos al cargar la página
 */
document.addEventListener('DOMContentLoaded', function () {
    // Cargar días seleccionados si estamos en modo edición
    const diasGuardados = document.getElementById('diasSeleccionados')?.value;
    if (diasGuardados) {
        cargarDiasSeleccionados(diasGuardados);
    }

    // Agregar event listeners a los checkboxes
    document.querySelectorAll('.dia-checkbox').forEach(function (checkbox) {
        checkbox.addEventListener('change', actualizarDiasSeleccionados);
    });

    // Validación antes de enviar el formulario
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('submit', validarFormulario);
    }
});

/**
 * Cargar días seleccionados desde una cadena de texto
 * Formatos soportados: "Lunes a Viernes", "Lunes, Martes", "Lunes y Martes"
 */
function cargarDiasSeleccionados(diasStr) {
    const todosDias = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes'];

    if (diasStr.includes(' a ')) {
        // Formato "Lunes a Viernes"
        const partes = diasStr.split(' a ');
        const inicio = partes[0].trim();
        const fin = partes[1].trim();
        const indiceInicio = todosDias.indexOf(inicio);
        const indiceFin = todosDias.indexOf(fin);

        for (let i = indiceInicio; i <= indiceFin; i++) {
            const checkbox = document.querySelector('.dia-checkbox[value="' + todosDias[i] + '"]');
            if (checkbox) checkbox.checked = true;
        }
    } else if (diasStr.includes(',')) {
        // Formato "Lunes, Martes, Miércoles"
        const dias = diasStr.split(',').map(d => d.trim());
        dias.forEach(function (dia) {
            const checkbox = document.querySelector('.dia-checkbox[value="' + dia + '"]');
            if (checkbox) checkbox.checked = true;
        });
    } else if (diasStr.includes(' y ')) {
        // Formato "Lunes y Martes"
        const dias = diasStr.split(' y ').map(d => d.trim());
        dias.forEach(function (dia) {
            const checkbox = document.querySelector('.dia-checkbox[value="' + dia + '"]');
            if (checkbox) checkbox.checked = true;
        });
    } else {
        // Un solo día
        const checkbox = document.querySelector('.dia-checkbox[value="' + diasStr.trim() + '"]');
        if (checkbox) checkbox.checked = true;
    }
}

/**
 * Actualizar el campo oculto con los días seleccionados
 */
function actualizarDiasSeleccionados() {
    const checkboxes = document.querySelectorAll('.dia-checkbox:checked');
    const diasSeleccionados = Array.from(checkboxes).map(cb => cb.value);

    let resultado = '';
    if (diasSeleccionados.length > 0) {
        // Formatear como "Lunes a Viernes" o "Lunes, Martes, Miércoles"
        if (esRangoConsecutivo(diasSeleccionados)) {
            resultado = diasSeleccionados[0] + ' a ' + diasSeleccionados[diasSeleccionados.length - 1];
        } else {
            resultado = diasSeleccionados.join(', ');
        }
    }

    document.getElementById('diasSeleccionados').value = resultado;
}

/**
 * Verificar si los días seleccionados forman un rango consecutivo
 */
function esRangoConsecutivo(dias) {
    const orden = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes'];
    if (dias.length < 2) return false;

    const indices = dias.map(d => orden.indexOf(d)).sort((a, b) => a - b);

    for (let i = 1; i < indices.length; i++) {
        if (indices[i] !== indices[i - 1] + 1) return false;
    }
    return true;
}

/**
 * Seleccionar días de Lunes a Viernes
 */
function seleccionarLunesViernes() {
    limpiarSeleccion();
    ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes'].forEach(function (dia) {
        const checkbox = document.querySelector('.dia-checkbox[value="' + dia + '"]');
        if (checkbox) checkbox.checked = true;
    });
    actualizarDiasSeleccionados();
}

/**
 * Seleccionar todos los días de la semana
 */
function seleccionarTodos() {
    document.querySelectorAll('.dia-checkbox').forEach(function (cb) {
        cb.checked = true;
    });
    actualizarDiasSeleccionados();
}

/**
 * Limpiar toda la selección
 */
function limpiarSeleccion() {
    document.querySelectorAll('.dia-checkbox').forEach(function (cb) {
        cb.checked = false;
    });
    actualizarDiasSeleccionados();
}

/**
 * Validar que al menos un día esté seleccionado antes de enviar
 */
function validarFormulario(e) {
    const diasSeleccionados = document.getElementById('diasSeleccionados').value;
    if (!diasSeleccionados) {
        e.preventDefault();
        alert('Por favor, seleccione al menos un día de atención.');
        return false;
    }
    return true;
}