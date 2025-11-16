// =========================================================
// SCRIPT PARA AGENDAR CITAS - CALENDARIO INTERACTIVO
// =========================================================

// Variables globales para almacenar información de la cita
var datosResumenCita = {
    especialidad: '',
    medico: '',
    fecha: '',
    horario: ''
};

/**
 * Seleccionar una fecha del calendario
 */
function seleccionarFecha(fecha) {
    // Validar que la fecha no sea posterior a 2030-12-31
    var fechaObj = new Date(fecha);
    var fechaLimite = new Date('2030-12-31');

    if (fechaObj > fechaLimite) {
        mostrarAlerta('No se pueden agendar citas posteriores al 31 de diciembre de 2030.', 'warning');
        return;
    }

    // Asignar la fecha seleccionada y enviar el formulario
    document.getElementById('fechaSeleccionada').value = fecha;
    document.getElementById('formAgendar').submit();
}

/**
 * Cambiar mes del calendario (navegación)
 */
function cambiarMes(incremento) {
    var mesActual = parseInt(document.getElementById('mesActual').value);
    var anioActual = parseInt(document.getElementById('anioActual').value);

    mesActual += incremento;

    // Ajustar año si cambiamos de diciembre a enero o viceversa
    if (mesActual > 12) {
        mesActual = 1;
        anioActual++;
    } else if (mesActual < 1) {
        mesActual = 12;
        anioActual--;
    }

    // VALIDACIÓN: No permitir navegar más allá de diciembre 2030
    if (anioActual > 2030 || (anioActual === 2030 && mesActual > 12)) {
        mostrarAlerta('No se pueden agendar citas posteriores al año 2030.', 'warning');
        return;
    }

    // VALIDACIÓN: No permitir navegar antes del mes actual
    var hoy = new Date();
    var mesHoy = hoy.getMonth() + 1;
    var anioHoy = hoy.getFullYear();

    if (anioActual < anioHoy || (anioActual === anioHoy && mesActual < mesHoy)) {
        mostrarAlerta('No se pueden agendar citas en fechas pasadas.', 'warning');
        return;
    }

    // Actualizar campos ocultos y enviar formulario
    document.getElementById('mesActual').value = mesActual;
    document.getElementById('anioActual').value = anioActual;
    document.getElementById('formAgendar').submit();
}

/**
 * Seleccionar un horario de la lista
 */
function seleccionarHorario(boton, hora) {
    // Remover la clase 'seleccionado' de todos los botones
    document.querySelectorAll('.horario-btn').forEach(function (btn) {
        btn.classList.remove('seleccionado');
    });

    // Agregar la clase 'seleccionado' al botón clickeado
    boton.classList.add('seleccionado');

    // Guardar el horario seleccionado
    datosResumenCita.horario = hora;
    document.getElementById('horarioSeleccionado').value = hora;

    // Habilitar el botón de confirmar
    document.getElementById('btnConfirmar').disabled = false;
}

/**
 * Obtener datos para el resumen de la cita
 */
function obtenerDatosResumen() {
    // Obtener especialidad
    var selectEspecialidad = document.getElementById('idEspecialidad');
    if (selectEspecialidad && selectEspecialidad.selectedIndex > 0) {
        datosResumenCita.especialidad = selectEspecialidad.options[selectEspecialidad.selectedIndex].text;
    }

    // Obtener médico
    var selectMedico = document.getElementById('idMedico');
    if (selectMedico && selectMedico.selectedIndex > 0) {
        datosResumenCita.medico = selectMedico.options[selectMedico.selectedIndex].text;
    }

    // Obtener fecha
    var inputFecha = document.getElementById('fechaSeleccionada');
    if (inputFecha && inputFecha.value) {
        var fecha = new Date(inputFecha.value);
        var opciones = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
        datosResumenCita.fecha = fecha.toLocaleDateString('es-ES', opciones);
    }
}

/**
 * Mostrar modal de confirmación con resumen
 */
function mostrarModalConfirmacion() {
    // Obtener los datos actuales
    obtenerDatosResumen();

    // Validar que todos los datos estén completos
    if (!datosResumenCita.horario) {
        mostrarAlerta('Por favor, seleccione un horario antes de confirmar la cita.', 'warning');
        return false;
    }

    // Actualizar el contenido del modal
    document.getElementById('modalEspecialidad').textContent = datosResumenCita.especialidad || '-';
    document.getElementById('modalMedico').textContent = datosResumenCita.medico || '-';
    document.getElementById('modalFecha').textContent = datosResumenCita.fecha || '-';
    document.getElementById('modalHorario').textContent = datosResumenCita.horario + ' - ' + calcularHoraFin(datosResumenCita.horario);

    // Mostrar el modal
    var modal = new bootstrap.Modal(document.getElementById('modalConfirmarCita'));
    modal.show();

    return false; // Prevenir el envío del formulario
}

/**
 * Calcular hora de fin (1 hora después)
 */
function calcularHoraFin(horaInicio) {
    if (!horaInicio) return '';

    var partes = horaInicio.split(':');
    var hora = parseInt(partes[0]);
    var minutos = partes[1];

    hora = (hora + 1) % 24;

    return (hora < 10 ? '0' : '') + hora + ':' + minutos;
}

/**
 * Confirmar la cita (submit del formulario)
 */
function confirmarCita() {
    // Cerrar el modal
    var modal = bootstrap.Modal.getInstance(document.getElementById('modalConfirmarCita'));
    if (modal) {
        modal.hide();
    }

    // Enviar el formulario
    document.getElementById('formConfirmar').submit();
}

/**
 * Mostrar alerta personalizada (opcional)
 */
function mostrarAlerta(mensaje, tipo) {
    // Puedes usar alert nativo o crear tu propia alerta
    alert(mensaje);
}

// =========================================================
// EVENTOS AL CARGAR LA PÁGINA
// =========================================================
document.addEventListener('DOMContentLoaded', function () {
    // Prevenir el envío directo del formulario y mostrar modal
    var formConfirmar = document.getElementById('formConfirmar');
    if (formConfirmar) {
        formConfirmar.addEventListener('submit', function (e) {
            e.preventDefault();
            mostrarModalConfirmacion();
        });
    }

    // Evento del botón de confirmar en el modal
    var btnConfirmarModal = document.getElementById('btnConfirmarModal');
    if (btnConfirmarModal) {
        btnConfirmarModal.addEventListener('click', function () {
            confirmarCita();
        });
    }

    console.log('Sistema de agendamiento de citas cargado correctamente');
});