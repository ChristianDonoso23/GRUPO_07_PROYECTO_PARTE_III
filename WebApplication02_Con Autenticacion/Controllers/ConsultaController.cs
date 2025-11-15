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

        // GET: Consulta 
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

            System.Diagnostics.Debug.WriteLine("Consultas cargadas: " + lista.Count);

            return View(lista);
        }

        // GET: Consulta/Details/5
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

        // GET: Consulta/Create
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

        // POST: Consulta/Create
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

        // GET: Consulta/Edit/5
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

        // POST: Consulta/Edit/5
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

        // GET: Consulta/Delete/5
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

        // POST: Consulta/Delete/5
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

        // GET: Consulta/MisCitas  → Para pacientes logueados
        [Authorize(Roles = "Paciente")]
        public ActionResult CitasPaciente()
        {
            var usuario = SessionHelper.CurrentUser;

            if (usuario == null)
                return RedirectToAction("Login", "Account");

            // Buscar el IdPaciente del usuario actual
            var idPaciente = db.pacientes
                .Where(p => p.IdUsuario == usuario.Id)
                .Select(p => p.IdPaciente)
                .FirstOrDefault();

            if (idPaciente == 0)
            {
                TempData["ErrorMensaje"] = "No se encontró un perfil de paciente asociado a este usuario.";
                return RedirectToAction("Index", "Home");
            }

            // 🔹 Incluye las relaciones para evitar proxies
            var consultas = db.consultas
                .Include(c => c.medicos)
                .Include(c => c.pacientes)
                .Where(c => c.IdPaciente == idPaciente)
                .AsNoTracking() // evita proxies dinámicos
                .ToList();

            return View("CitasPaciente", consultas);
        }

        [Authorize(Roles = "Paciente")]
        public ActionResult Agendar(int? idEspecialidad, int? idMedico, DateTime? fecha)
        {
            var usuario = SessionHelper.CurrentUser;

            if (usuario == null)
                return RedirectToAction("Login", "Account");

            // 1. Especialidades
            ViewBag.Especialidades = new SelectList(db.especialidades, "IdEspecialidad", "Descripcion", idEspecialidad);

            // 2. Médicos según especialidad (solo si ya eligió especialidad)
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

            // 3. Fecha seleccionada
            ViewBag.FechaSeleccionada = fecha?.ToString("yyyy-MM-dd");

            // 4. Horarios disponibles (si ya eligió médico y fecha)
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

        private List<string> ObtenerHorariosDisponibles(int idMedico, DateTime fecha)
        {
            List<string> horarios = new List<string>();

            // 1. No fines de semana
            if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday)
                return horarios;

            // 2. Médico y especialidad
            var medico = db.medicos.Find(idMedico);
            var especialidad = db.especialidades.Find(medico.IdEspecialidad);

            // Validar que trabaje ese día
            if (!EspecialidadTrabajaEseDia(especialidad, fecha))
                return horarios;

            // 3. Rango de horario (intersección clínica/especialidad)
            TimeSpan inicio = especialidad.Franja_HI < TimeSpan.FromHours(8)
                ? TimeSpan.FromHours(8)
                : especialidad.Franja_HI;

            TimeSpan fin = especialidad.Franja_HF > TimeSpan.FromHours(18)
                ? TimeSpan.FromHours(18)
                : especialidad.Franja_HF;

            // 4. Horas ocupadas
            var ocupadas = db.consultas
                .Where(c => c.IdMedico == idMedico && c.FechaConsulta == fecha)
                .Select(c => c.HI)
                .ToList();

            // 5. Crear slots de 1 hora
            for (var hora = inicio; hora < fin; hora = hora.Add(TimeSpan.FromHours(1)))
            {
                if (!ocupadas.Contains(hora))
                    horarios.Add(hora.ToString(@"hh\:mm"));
            }

            return horarios;
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        public ActionResult AgendarConfirmado(int IdEspecialidad, int IdMedico, DateTime FechaConsulta, string HI)
        {
            var usuario = SessionHelper.CurrentUser;

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

        private bool EspecialidadTrabajaEseDia(especialidades esp, DateTime fecha)
        {
            string dia = DiaEnEspanol(fecha.DayOfWeek);
            string dias = esp.Dias;

            // Caso 1: rango tipo “Lunes a Viernes”
            if (dias.Contains(" a "))
            {
                var partes = dias.Split(new string[] { " a " }, StringSplitOptions.None);
                string inicio = partes[0].Trim();
                string fin = partes[1].Trim();

                var orden = new List<string>
        {
            "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"
        };

                int posDia = orden.IndexOf(dia);
                int posInicio = orden.IndexOf(inicio);
                int posFin = orden.IndexOf(fin);

                return posDia >= posInicio && posDia <= posFin;
            }

            // Caso 2: lista separada por comas: “Lunes, Miércoles, Viernes”
            if (dias.Contains(","))
            {
                var lista = dias.Split(',')
                                .Select(d => d.Trim())
                                .ToList();

                return lista.Contains(dia);
            }

            // Caso 3: “Martes y Jueves”
            if (dias.Contains(" y "))
            {
                var lista = dias.Split(new string[] { " y " }, StringSplitOptions.None)
                                .Select(d => d.Trim())
                                .ToList();

                return lista.Contains(dia);
            }

            // Caso 4: día único
            return dias.Trim() == dia;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
