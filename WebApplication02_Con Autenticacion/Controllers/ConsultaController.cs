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
            var usuario = SessionHelper.CurrentUser;
            IQueryable<consultas> consultasQuery;

            if (usuario == null)
                return RedirectToAction("Login", "Account");

            if (User.IsInRole("SuperAdmin") || User.IsInRole("Administrador"))
            {
                consultasQuery = db.consultas;
            }
            else if (User.IsInRole("Medico"))
            {
                var idMedico = db.medicos
                    .Where(m => m.IdUsuario == usuario.Id)
                    .Select(m => m.IdMedico)
                    .FirstOrDefault();

                consultasQuery = db.consultas.Where(c => c.IdMedico == idMedico);
            }
            else
            {
                consultasQuery = Enumerable.Empty<consultas>().AsQueryable();
            }

            var lista = consultasQuery
                .ToList()
                .Select(c => new consultas
                {
                    IdConsulta = c.IdConsulta,
                    IdMedico = c.IdMedico,
                    IdPaciente = c.IdPaciente,
                    FechaConsulta = c.FechaConsulta,
                    HI = c.HI,
                    HF = c.HF,
                    Diagnostico = c.Diagnostico,
                    pacientes = db.pacientes.FirstOrDefault(p => p.IdPaciente == c.IdPaciente),
                    medicos = db.medicos.FirstOrDefault(m => m.IdMedico == c.IdMedico)
                })
                .ToList();

            return View(lista);
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
        // CREAR CONSULTA
        // =====================================================================
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create()
        {
            var usuario = SessionHelper.CurrentUser;

            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre");

            if (usuario != null && User.IsInRole("Medico") && !User.IsInRole("SuperAdmin"))
            {
                var idMedico = db.medicos
                    .Where(m => m.IdUsuario == usuario.Id)
                    .Select(m => m.IdMedico)
                    .FirstOrDefault();

                ViewBag.IdMedico = new SelectList(
                    db.medicos.Where(m => m.IdMedico == idMedico),
                    "IdMedico", "Nombre"
                );
            }
            else
            {
                ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create([Bind(Include = "IdConsulta,IdMedico,IdPaciente,FechaConsulta,HI,HF,Diagnostico")] consultas consulta)
        {
            var usuario = SessionHelper.CurrentUser;

            if (usuario != null && User.IsInRole("Medico") && !User.IsInRole("SuperAdmin"))
            {
                var idMedico = db.medicos
                    .Where(m => m.IdUsuario == usuario.Id)
                    .Select(m => m.IdMedico)
                    .FirstOrDefault();

                consulta.IdMedico = idMedico;
            }

            ViewBag.IdPaciente = new SelectList(db.pacientes, "IdPaciente", "Nombre", consulta.IdPaciente);
            ViewBag.IdMedico = new SelectList(db.medicos, "IdMedico", "Nombre", consulta.IdMedico);

            if (!consulta.HorarioValido())
                ModelState.AddModelError("HF", "La hora de fin debe ser mayor que la hora de inicio.");

            if (!consulta.FechaValida())
                ModelState.AddModelError("FechaConsulta", "La fecha de la consulta no puede ser futura.");

            if (!ModelState.IsValid)
                return View(consulta);

            db.consultas.Add(consulta);
            db.SaveChanges();

            return RedirectToAction("Index");
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
        // AGENDAR CONSULTA (GET) — ***MODIFICADO PARA NAVEGAR ENTRE MESES***
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

            // ========================================================
            // CALENDARIO — MES / AÑO (NAVEGACIÓN)
            // ========================================================
            DateTime hoy = DateTime.Now;

            int mesActual = mes ?? hoy.Month;
            int anioActual = anio ?? hoy.Year;

            ViewBag.MesActual = mesActual;
            ViewBag.Anio = anioActual;
            ViewBag.NombreMes = new DateTime(anioActual, mesActual, 1).ToString("MMMM");

            // Días disponibles según especialidad
            if (idEspecialidad.HasValue)
                ViewBag.DiasLaborables = ObtenerDiasDisponiblesMes(idEspecialidad.Value, mesActual, anioActual);
            else
                ViewBag.DiasLaborables = new List<string>();

            // Mini calendario
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
        // HORARIOS DISPONIBLES
        // =====================================================================
        private List<string> ObtenerHorariosDisponibles(int idMedico, DateTime fecha)
        {
            List<string> horarios = new List<string>();

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

            int inicioSemana = ((int)primerDia.DayOfWeek == 0) ? 7 : (int)primerDia.DayOfWeek;

            List<string> semana = new List<string>();

            for (int i = 1; i < inicioSemana; i++)
                semana.Add("");

            for (int dia = 1; dia <= diasEnMes; dia++)
            {
                DateTime fecha = new DateTime(year, month, dia);

                if (fecha < DateTime.Today)
                {
                    semana.Add("PAST-" + fecha.ToString("yyyy-MM-dd"));
                }
                else
                {
                    semana.Add(fecha.ToString("yyyy-MM-dd"));
                }

                if (semana.Count == 7)
                {
                    calendario.Add(semana);
                    semana = new List<string>();
                }
            }

            if (semana.Count > 0)
            {
                while (semana.Count < 7)
                    semana.Add("");

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
            string dia = DiaEnEspanol(fecha.DayOfWeek);
            string dias = esp.Dias;

            if (dias.Contains(" a "))
            {
                var partes = dias.Split(new string[] { " a " }, StringSplitOptions.None);
                string inicio = partes[0].Trim();
                string fin = partes[1].Trim();

                var orden = new List<string>
                {
                    "Lunes","Martes","Miércoles","Jueves","Viernes","Sábado","Domingo"
                };

                int posDia = orden.IndexOf(dia);
                int posInicio = orden.IndexOf(inicio);
                int posFin = orden.IndexOf(fin);

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
