using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication02_Con_Autenticacion.Models;
using WebApplication02_Con_Autenticacion.Helpers;

namespace WebApplication02_Con_Autenticacion.Controllers
{
    public class ConsultaController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // =====================================================================
        // INDEX
        // =====================================================================
        [Authorize(Roles = "SuperAdmin, Administrador, Medico")]
        public ActionResult Index()
        {
            try
            {
                // --------------------------
                // VALIDACIÓN DE SESIÓN
                // --------------------------
                var usuario = SessionHelper.CurrentUser;
                if (usuario == null)
                    return RedirectToAction("Login", "Account");

                IQueryable<consultas> consultasQuery;

                // --------------------------
                // ROLES: ADMINISTRADORES
                // --------------------------
                if (User.IsInRole("SuperAdmin") || User.IsInRole("Administrador"))
                {
                    consultasQuery = db.consultas
                        .Include(c => c.pacientes)
                        .Include(c => c.medicos);
                }
                // --------------------------
                // ROL: MÉDICO
                // --------------------------
                else if (User.IsInRole("Medico"))
                {
                    var idMedico = db.medicos
                        .Where(m => m.IdUsuario == usuario.Id)
                        .Select(m => m.IdMedico)
                        .FirstOrDefault();

                    if (idMedico == 0)
                    {
                        // Médico sin registro → lista vacía
                        consultasQuery = Enumerable.Empty<consultas>().AsQueryable();
                    }
                    else
                    {
                        consultasQuery = db.consultas
                            .Include(c => c.pacientes)
                            .Include(c => c.medicos)
                            .Where(c => c.IdMedico == idMedico);
                    }
                }
                // --------------------------
                // OTROS ROLES (no permitidos)
                // --------------------------
                else
                {
                    consultasQuery = Enumerable.Empty<consultas>().AsQueryable();
                }

                // EJECUCIÓN FINAL (ya con Includes)
                var lista = consultasQuery.ToList();

                return View(lista);
            }
            catch (Exception ex)
            {
                var errorInfo = new HandleErrorInfo(ex, "Consultas", "Index");
                return View("Error", errorInfo);
            }
        }


        // =====================================================================
        // DETALLES
        // =====================================================================
        [Authorize(Roles = "SuperAdmin, Administrador, Medico")]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            consultas consulta = db.consultas.Find(id);
            if (consulta == null)
                return HttpNotFound();

            consulta.medicos = db.medicos.FirstOrDefault(m => m.IdMedico == consulta.IdMedico);
            consulta.pacientes = db.pacientes.FirstOrDefault(p => p.IdPaciente == consulta.IdPaciente);

            return View(consulta);
        }

        // =====================================================================
        // CREAR CONSULTA (GET) - CON HORARIOS DINÁMICOS
        // =====================================================================
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create(int? idMedico, int? idPaciente, DateTime? fecha)
        {
            var usuario = SessionHelper.CurrentUser;

            // Mantener el paciente seleccionado
            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre", idPaciente);

            if (usuario != null && User.IsInRole("Medico") && !User.IsInRole("SuperAdmin"))
            {
                var idMedicoActual = db.medicos
                    .Where(m => m.IdUsuario == usuario.Id)
                    .Select(m => m.IdMedico)
                    .FirstOrDefault();

                ViewBag.IdMedico = new SelectList(
                    db.medicos.Where(m => m.IdMedico == idMedicoActual),
                    "IdMedico", "Nombre", idMedicoActual
                );
            }
            else
            {
                ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre", idMedico);
            }

            ViewBag.FechaSeleccionada = fecha?.ToString("yyyy-MM-dd");

            // Generar horarios disponibles si médico + fecha seleccionados
            if (idMedico.HasValue && fecha.HasValue)
            {
                ViewBag.Horarios = ObtenerHorariosDisponiblesParaCrear(idMedico.Value, fecha.Value);
            }
            else
            {
                ViewBag.Horarios = new List<string>();
            }

            return View();
        }

        // =====================================================================
        // CREAR CONSULTA (POST)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create(int IdMedico, int IdPaciente, DateTime FechaConsulta, string HI, string Diagnostico)
        {
            var usuario = SessionHelper.CurrentUser;

            // Si es médico sin privilegios de SuperAdmin, usar su propio ID
            if (usuario != null && User.IsInRole("Medico") && !User.IsInRole("SuperAdmin"))
            {
                IdMedico = db.medicos
                    .Where(m => m.IdUsuario == usuario.Id)
                    .Select(m => m.IdMedico)
                    .FirstOrDefault();
            }

            // VALIDACIÓN 1: Rango de años permitidos (2000-2030)
            if (FechaConsulta.Year < 2000 || FechaConsulta.Year > 2030)
            {
                TempData["Error"] = "Solo se pueden registrar consultas entre los años 2000 y 2030.";
                return RedirectToAction("Create", new { idMedico = IdMedico, fecha = FechaConsulta.ToString("yyyy-MM-dd") });
            }

            // VALIDACIÓN 2: La fecha DEBE ser desde hoy en adelante
            if (FechaConsulta.Date < DateTime.Now.Date)
            {
                TempData["Error"] = "No se pueden registrar consultas con fechas pasadas.";
                return RedirectToAction("Create", new { idMedico = IdMedico, fecha = FechaConsulta.ToString("yyyy-MM-dd") });
            }

            // VALIDACIÓN 3: No permitir sábados y domingos
            if (FechaConsulta.DayOfWeek == DayOfWeek.Saturday || FechaConsulta.DayOfWeek == DayOfWeek.Sunday)
            {
                TempData["Error"] = "No se pueden registrar consultas en sábados o domingos.";
                return RedirectToAction("Create", new { idMedico = IdMedico, fecha = FechaConsulta.ToString("yyyy-MM-dd") });
            }

            TimeSpan horaInicio = TimeSpan.Parse(HI);
            TimeSpan horaFin = horaInicio.Add(TimeSpan.FromHours(1));

            // VALIDACIÓN 4: Verificar que el horario esté dentro del rango permitido (8:00 - 18:00)
            TimeSpan horaMinima = TimeSpan.FromHours(8);
            TimeSpan horaMaxima = TimeSpan.FromHours(18);

            if (horaInicio < horaMinima || horaInicio >= horaMaxima)
            {
                TempData["Error"] = "La hora de inicio debe estar entre las 08:00 y las 18:00.";
                return RedirectToAction("Create", new { idMedico = IdMedico, fecha = FechaConsulta.ToString("yyyy-MM-dd") });
            }

            // VALIDACIÓN 5: Verificar que no haya conflictos de horario
            var consultasExistentes = db.consultas
                .Where(c => c.IdMedico == IdMedico && c.FechaConsulta == FechaConsulta && c.HI == horaInicio)
                .Any();

            if (consultasExistentes)
            {
                TempData["Error"] = "Ya existe una consulta en ese horario para este médico.";
                return RedirectToAction("Create", new { idMedico = IdMedico, fecha = FechaConsulta.ToString("yyyy-MM-dd") });
            }

            consultas consulta = new consultas
            {
                IdMedico = IdMedico,
                IdPaciente = IdPaciente,
                FechaConsulta = FechaConsulta,
                HI = horaInicio,
                HF = horaFin,
                Diagnostico = Diagnostico
            };

            db.consultas.Add(consulta);
            db.SaveChanges();

            TempData["Mensaje"] = "Consulta registrada correctamente.";
            return RedirectToAction("Index");
        }

        // =====================================================================
        // HORARIOS DISPONIBLES PARA CREAR (permite fechas pasadas)
        // =====================================================================
        private List<string> ObtenerHorariosDisponiblesParaCrear(int idMedico, DateTime fecha)
        {
            List<string> horarios = new List<string>();

            // PERMITIR SOLO FECHAS DESDE HOY EN ADELANTE
            if (fecha < DateTime.Today)
                return horarios;

            if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                return horarios;

            var medico = db.medicos.Find(idMedico);

            if (medico == null)
                return horarios;

            var especialidad = db.especialidades.Find(medico.IdEspecialidad);

            if (especialidad == null)
                return horarios;

            if (!EspecialidadTrabajaEseDia(especialidad, fecha))
                return horarios;

            TimeSpan inicio = especialidad.Franja_HI < TimeSpan.FromHours(8)
                ? TimeSpan.FromHours(8)
                : especialidad.Franja_HI;

            TimeSpan fin = especialidad.Franja_HF > TimeSpan.FromHours(18)
                ? TimeSpan.FromHours(18)
                : especialidad.Franja_HF;

            var ocupadas = db.consultas
                .Where(c => c.IdMedico == idMedico && c.FechaConsulta == fecha)
                .Select(c => c.HI)
                .ToList();

            for (var hora = inicio; hora < fin; hora = hora.Add(TimeSpan.FromHours(1)))
            {
                if (!ocupadas.Contains(hora))
                    horarios.Add(hora.ToString(@"hh\:mm"));
            }

            return horarios;
        }

        // =====================================================================
        // EDITAR CONSULTA
        // =====================================================================
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            consultas consulta = db.consultas.Find(id);
            if (consulta == null)
                return HttpNotFound();

            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre", consulta.IdPaciente);
            ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre", consulta.IdMedico);
            return View(consulta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Edit([Bind(Include = "IdConsulta,IdMedico,IdPaciente,FechaConsulta,HI,HF,Diagnostico")] consultas consulta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(consulta).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre", consulta.IdPaciente);
            ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre", consulta.IdMedico);
            return View(consulta);
        }

        // =====================================================================
        // ELIMINAR CONSULTA
        // =====================================================================
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            consultas consulta = db.consultas.Find(id);
            if (consulta == null)
                return HttpNotFound();

            consulta.medicos = db.medicos.FirstOrDefault(m => m.IdMedico == consulta.IdMedico);
            consulta.pacientes = db.pacientes.FirstOrDefault(p => p.IdPaciente == consulta.IdPaciente);

            return View(consulta);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult DeleteConfirmed(int id)
        {
            consultas consulta = db.consultas.Find(id);
            db.consultas.Remove(consulta);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // =====================================================================
        // CITAS PACIENTE
        // =====================================================================
        [Authorize(Roles = "Paciente")]
        public ActionResult CitasPaciente()
        {
            var usuario = SessionHelper.CurrentUser;

            if (usuario == null)
                return RedirectToAction("Login", "Account");

            var idPaciente = db.pacientes
                .Where(p => p.IdUsuario == usuario.Id)
                .Select(p => p.IdPaciente)
                .FirstOrDefault();

            if (idPaciente == 0)
            {
                TempData["ErrorMensaje"] = "No se encontró un perfil de paciente asociado a este usuario.";
                return RedirectToAction("Index", "Home");
            }

            var consultas = db.consultas
                .Include(c => c.medicos)
                .Include(c => c.pacientes)
                .Where(c => c.IdPaciente == idPaciente)
                .AsNoTracking()
                .ToList();

            return View("CitasPaciente", consultas);
        }

        // =====================================================================
        // AGENDAR CONSULTA (GET)
        // =====================================================================
        [Authorize(Roles = "Paciente")]
        public ActionResult Agendar(int? idEspecialidad, int? idMedico, DateTime? fecha, int? mes, int? anio)
        {
            var usuario = SessionHelper.CurrentUser;
            if (usuario == null)
                return RedirectToAction("Login", "Account");

            // SELECTS
            ViewBag.Especialidades = new SelectList(db.especialidades, "IdEspecialidad", "Descripcion", idEspecialidad);

            if (idEspecialidad.HasValue)
            {
                var listaMedicos = db.medicos
                    .Where(m => m.IdEspecialidad == idEspecialidad.Value)
                    .ToList();

                ViewBag.Medicos = new SelectList(listaMedicos, "IdMedico", "Nombre", idMedico);
            }
            else
            {
                ViewBag.Medicos = new SelectList(new List<medicos>(), "IdMedico", "Nombre");
            }

            ViewBag.FechaSeleccionada = fecha?.ToString("yyyy-MM-dd");

            // CALENDARIO
            DateTime hoy = DateTime.Now;
            int mesActual = mes ?? hoy.Month;
            int anioActual = anio ?? hoy.Year;

            //VALIDACIÓN: No permitir años mayores a 2030
            if (anioActual > 2030)
            {
                anioActual = 2030;
                mesActual = 12;
            }

            // VALIDACIÓN: No permitir años menores a año actual
            if (anioActual < hoy.Year)
            {
                anioActual = hoy.Year;
                mesActual = hoy.Month;
            }

            ViewBag.MesActual = mesActual;
            ViewBag.Anio = anioActual;
            ViewBag.NombreMes = new DateTime(anioActual, mesActual, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));

            // Verificar si estamos en diciembre 2030 (para deshabilitar botón siguiente)
            ViewBag.EsUltimoMes = (anioActual == 2030 && mesActual == 12);

            // Verificar si estamos en el mes actual (para deshabilitar botón anterior)
            ViewBag.EsPrimerMes = (anioActual == hoy.Year && mesActual == hoy.Month);

            // Obtener días con horarios disponibles
            Dictionary<string, int> diasConHorarios = new Dictionary<string, int>();

            if (idEspecialidad.HasValue && idMedico.HasValue)
            {
                var esp = db.especialidades.Find(idEspecialidad.Value);
                int diasEnMes = DateTime.DaysInMonth(anioActual, mesActual);

                for (int d = 1; d <= diasEnMes; d++)
                {
                    DateTime fechaTemp = new DateTime(anioActual, mesActual, d);

                    // No incluir fechas posteriores a 2030-12-31
                    if (fechaTemp.Year > 2030 || (fechaTemp.Year == 2030 && fechaTemp.Month == 12 && fechaTemp.Day > 31))
                        continue;

                    if (fechaTemp >= DateTime.Today &&
                        fechaTemp.DayOfWeek != DayOfWeek.Saturday &&
                        fechaTemp.DayOfWeek != DayOfWeek.Sunday &&
                        EspecialidadTrabajaEseDia(esp, fechaTemp))
                    {
                        var horariosDisponibles = ObtenerHorariosDisponibles(idMedico.Value, fechaTemp);
                        if (horariosDisponibles.Count > 0)
                        {
                            diasConHorarios[fechaTemp.ToString("yyyy-MM-dd")] = horariosDisponibles.Count;
                        }
                    }
                }
            }

            ViewBag.DiasConHorarios = diasConHorarios;
            ViewBag.Calendario = GenerarCalendario(anioActual, mesActual);

            // Generar horarios si médico + fecha seleccionados
            if (idMedico.HasValue && fecha.HasValue)
            {
                ViewBag.Horarios = ObtenerHorariosDisponibles(idMedico.Value, fecha.Value);
            }
            else
            {
                ViewBag.Horarios = new List<string>();
            }

            return View();
        }

        // =====================================================================
        // HORARIOS DISPONIBLES PARA AGENDAR (solo fechas futuras)
        // =====================================================================
        private List<string> ObtenerHorariosDisponibles(int idMedico, DateTime fecha)
        {
            List<string> horarios = new List<string>();

            // Validación: Solo fechas desde hoy en adelante
            if (fecha < DateTime.Today)
                return horarios;

            if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                return horarios;

            var medico = db.medicos.Find(idMedico);
            var especialidad = db.especialidades.Find(medico.IdEspecialidad);

            if (!EspecialidadTrabajaEseDia(especialidad, fecha))
                return horarios;

            TimeSpan inicio = especialidad.Franja_HI < TimeSpan.FromHours(8)
                ? TimeSpan.FromHours(8)
                : especialidad.Franja_HI;

            TimeSpan fin = especialidad.Franja_HF > TimeSpan.FromHours(18)
                ? TimeSpan.FromHours(18)
                : especialidad.Franja_HF;

            var ocupadas = db.consultas
                .Where(c => c.IdMedico == idMedico && c.FechaConsulta == fecha)
                .Select(c => c.HI)
                .ToList();

            for (var hora = inicio; hora < fin; hora = hora.Add(TimeSpan.FromHours(1)))
            {
                if (!ocupadas.Contains(hora))
                    horarios.Add(hora.ToString(@"hh\:mm"));
            }

            return horarios;
        }

        // =====================================================================
        // AGENDAR CONFIRMADO
        // =====================================================================
        [HttpPost]
        [Authorize(Roles = "Paciente")]
        public ActionResult AgendarConfirmado(int IdEspecialidad, int IdMedico, DateTime FechaConsulta, string HI)
        {
            var usuario = SessionHelper.CurrentUser;

            if (FechaConsulta < DateTime.Today)
            {
                TempData["Error"] = "No puede agendar fechas pasadas.";
                return RedirectToAction("Agendar", new { IdEspecialidad, IdMedico });
            }

            if (FechaConsulta.Year < 2000 || FechaConsulta.Year > 2030)
            {
                TempData["Error"] = "Solo se pueden agendar citas entre los años 2000 y 2030.";
                return RedirectToAction("Agendar", new { idEspecialidad = IdEspecialidad, idMedico = IdMedico });
            }

            var idPaciente = db.pacientes
                .Where(p => p.IdUsuario == usuario.Id)
                .Select(p => p.IdPaciente)
                .FirstOrDefault();

            TimeSpan horaInicio = TimeSpan.Parse(HI);
            TimeSpan horaFin = horaInicio.Add(TimeSpan.FromHours(1));

            consultas cita = new consultas
            {
                IdMedico = IdMedico,
                IdPaciente = idPaciente,
                FechaConsulta = FechaConsulta,
                HI = horaInicio,
                HF = horaFin,
                Diagnostico = "Cita programada"
            };

            db.consultas.Add(cita);
            db.SaveChanges();

            return RedirectToAction("CitasPaciente");
        }

        // =====================================================================
        // GENERAR CALENDARIO (CON DÍAS PASADOS EN GRIS)
        // =====================================================================
        private List<List<string>> GenerarCalendario(int year, int month)
        {
            List<List<string>> calendario = new List<List<string>>();

            DateTime primerDia = new DateTime(year, month, 1);
            int diasEnMes = DateTime.DaysInMonth(year, month);

            // Obtener el día de la semana del primer día (Lunes = 1, Domingo = 7)
            int diaSemana = (int)primerDia.DayOfWeek;
            if (diaSemana == 0) diaSemana = 7;

            List<string> semana = new List<string>();

            // Llenar días vacíos antes del primer día del mes
            for (int i = 1; i < diaSemana; i++)
            {
                semana.Add("");
            }

            // Agregar todos los días del mes
            for (int dia = 1; dia <= diasEnMes; dia++)
            {
                DateTime fecha = new DateTime(year, month, dia);
                semana.Add(fecha.ToString("yyyy-MM-dd"));

                // Si completamos 7 días, guardamos la semana y empezamos otra
                if (semana.Count == 7)
                {
                    calendario.Add(new List<string>(semana));
                    semana = new List<string>();
                }
            }

            // Completar la última semana con días vacíos si es necesario
            if (semana.Count > 0)
            {
                while (semana.Count < 7)
                {
                    semana.Add("");
                }
                calendario.Add(semana);
            }

            return calendario;
        }

        // =====================================================================
        // DÍAS LABORABLES SEGÚN MES
        // =====================================================================
        private List<string> ObtenerDiasDisponiblesMes(int idEspecialidad, int mes, int anio)
        {
            List<string> dias = new List<string>();

            var esp = db.especialidades.Find(idEspecialidad);
            if (esp == null)
                return dias;

            int diasEnMes = DateTime.DaysInMonth(anio, mes);

            for (int d = 1; d <= diasEnMes; d++)
            {
                DateTime fecha = new DateTime(anio, mes, d);

                if (fecha < DateTime.Today)
                    continue;

                if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                if (!EspecialidadTrabajaEseDia(esp, fecha))
                    continue;

                dias.Add(fecha.ToString("yyyy-MM-dd"));
            }

            return dias;
        }

        // =====================================================================
        // LOGICA DE DIAS QUE TRABAJA LA ESPECIALIDAD
        // =====================================================================
        private bool EspecialidadTrabajaEseDia(especialidades esp, DateTime fecha)
        {
            if (esp == null || string.IsNullOrWhiteSpace(esp.Dias))
                return false;

            string dia = DiaEnEspanol(fecha.DayOfWeek);
            string dias = esp.Dias;

            if (dias.Contains(" a "))
            {
                var partes = dias.Split(new string[] { " a " }, StringSplitOptions.None);
                if (partes.Length < 2)
                    return false;

                string inicio = partes[0].Trim();
                string fin = partes[1].Trim();

                var orden = new List<string>
                {
                    "Lunes","Martes","Miércoles","Jueves","Viernes","Sábado","Domingo"
                };

                int posDia = orden.IndexOf(dia);
                int posInicio = orden.IndexOf(inicio);
                int posFin = orden.IndexOf(fin);

                if (posDia == -1 || posInicio == -1 || posFin == -1)
                    return false;

                return posDia >= posInicio && posDia <= posFin;
            }

            if (dias.Contains(","))
            {
                var lista = dias.Split(',').Select(d => d.Trim()).ToList();
                return lista.Contains(dia);
            }

            if (dias.Contains(" y "))
            {
                var lista = dias.Split(new string[] { " y " }, StringSplitOptions.None)
                                .Select(d => d.Trim()).ToList();

                return lista.Contains(dia);
            }

            return dias.Trim() == dia;
        }

        private string DiaEnEspanol(DayOfWeek dia)
        {
            switch (dia)
            {
                case DayOfWeek.Monday: return "Lunes";
                case DayOfWeek.Tuesday: return "Martes";
                case DayOfWeek.Wednesday: return "Miércoles";
                case DayOfWeek.Thursday: return "Jueves";
                case DayOfWeek.Friday: return "Viernes";
                case DayOfWeek.Saturday: return "Sábado";
                case DayOfWeek.Sunday: return "Domingo";
            }
            return "";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}