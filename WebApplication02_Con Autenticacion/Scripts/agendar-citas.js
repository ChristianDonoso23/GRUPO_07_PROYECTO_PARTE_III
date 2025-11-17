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
    // Validar fecha límite
    var fechaObj = new Date(fecha);
    var fechaLimite = new Date('2030-12-31');

    if (fechaObj > fechaLimite) {
        mostrarAlerta('No se pueden agendar citas posteriores al 31 de diciembre de 2030.', 'warning');
        return;
    }

    // Guardar fecha y enviar formulario
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

    if (mesActual > 12) { mesActual = 1; anioActual++; }
    else if (mesActual < 1) { mesActual = 12; anioActual--; }

    if (anioActual > 2030) {
        mostrarAlerta('No se pueden agendar citas posteriores al año 2030.', 'warning');
        return;
    }

    var hoy = new Date();
    var mesHoy = hoy.getMonth() + 1;
    var anioHoy = hoy.getFullYear();

    if (anioActual < anioHoy || (anioActual === anioHoy && mesActual < mesHoy)) {
        mostrarAlerta('No se pueden agendar citas en fechas pasadas.', 'warning');
        return;
    }

    document.getElementById('mesActual').value = mesActual;
    document.getElementById('anioActual').value = anioActual;
    document.getElementById('formAgendar').submit();
}

/**
 * Seleccionar un horario
 */
function seleccionarHorario(boton, hora) {
    document.querySelectorAll('.horario-btn').forEach(function (btn) {
        btn.classList.remove('seleccionado');
    });

    boton.classList.add('seleccionado');

    datosResumenCita.horario = hora;
    document.getElementById('horarioSeleccionado').value = hora;

    document.getElementById('btnConfirmar').disabled = false;
}

/**
 * Obtener los datos del resumen de cita
 */
function obtenerDatosResumen() {
    var selectEspecialidad = document.getElementById('idEspecialidad');
    if (selectEspecialidad && selectEspecialidad.selectedIndex > 0) {
        datosResumenCita.especialidad = selectEspecialidad.options[selectEspecialidad.selectedIndex].text;
    }

    var selectMedico = document.getElementById('idMedico');
    if (selectMedico && selectMedico.selectedIndex > 0) {
        datosResumenCita.medico = selectMedico.options[selectMedico.selectedIndex].text;
    }

    var inputFecha = document.getElementById('fechaSeleccionada');
    if (inputFecha && inputFecha.value) {
        datosResumenCita.fecha = formatearFechaSinDia(inputFecha.value);
    }
}

/**
 * Formatear fecha SOLO como: "20 de noviembre de 2025"
 * SIN weekday, SIN timezone
 */
function formatearFechaSinDia(fechaStr) {
    const partes = fechaStr.split("-");
    const year = partes[0];
    const month = partes[1];
    const day = partes[2];

    const meses = [
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
    ];

    const nombreMes = meses[parseInt(month) - 1];

    return `${day} de ${nombreMes} de ${year}`;
}

/**
 * Mostrar modal con resumen
 */
function mostrarModalConfirmacion() {
    obtenerDatosResumen();

    if (!datosResumenCita.horario) {
        mostrarAlerta('Por favor, seleccione un horario antes de confirmar la cita.', 'warning');
        return false;
    }

    document.getElementById('modalEspecialidad').textContent = datosResumenCita.especialidad || '-';
    document.getElementById('modalMedico').textContent = datosResumenCita.medico || '-';
    document.getElementById('modalFecha').textContent = datosResumenCita.fecha || '-';
    document.getElementById('modalHorario').textContent =
        datosResumenCita.horario + ' - ' + calcularHoraFin(datosResumenCita.horario);

    var modal = new bootstrap.Modal(document.getElementById('modalConfirmarCita'));
    modal.show();

    return false;
}

/**
 * Calcular hora de fin (sumar 1 hora)
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
 * Confirmar cita
 */
function confirmarCita() {
    var modal = bootstrap.Modal.getInstance(document.getElementById('modalConfirmarCita'));
    if (modal) modal.hide();

    document.getElementById('formConfirmar').submit();
}

/**
 * Alertas (simple)
 */
function mostrarAlerta(mensaje, tipo) {
    alert(mensaje);
}

// =========================================================
// EVENTOS AL CARGAR
// =========================================================
document.addEventListener('DOMContentLoaded', function () {
    var formConfirmar = document.getElementById('formConfirmar');
    if (formConfirmar) {
        formConfirmar.addEventListener('submit', function (e) {
            e.preventDefault();
            mostrarModalConfirmacion();
        });
    }

    var btnConfirmarModal = document.getElementById('btnConfirmarModal');
    if (btnConfirmarModal) {
        btnConfirmarModal.addEventListener('click', function () {
            confirmarCita();
        });
    }

    console.log('Sistema de agendamiento cargado correctamente');
});
