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


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
