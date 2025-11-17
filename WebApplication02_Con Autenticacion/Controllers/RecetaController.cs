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
    public class RecetaController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        [Authorize(Roles = "SuperAdmin, Administrador, Medico")]
        public ActionResult Index()
        {
            try
            {
                // -------------------------
                // VALIDACIÓN DE SESIÓN
                // -------------------------
                var usuario = SessionHelper.CurrentUser;
                if (usuario == null)
                    return RedirectToAction("Login", "Account");

                IQueryable<recetas> recetasQuery;

                // -------------------------
                // ROLES: ADMINISTRADORES
                // -------------------------
                if (User.IsInRole("SuperAdmin") || User.IsInRole("Administrador"))
                {
                    recetasQuery = db.recetas
                        .Include(r => r.consultas)
                        .Include(r => r.medicamentos);
                }
                // -------------------------
                // ROLES: MÉDICO
                // -------------------------
                else if (User.IsInRole("Medico"))
                {
                    // Buscar el IdMedico del usuario logueado
                    var idMedico = db.medicos
                        .Where(m => m.IdUsuario == usuario.Id)
                        .Select(m => m.IdMedico)
                        .FirstOrDefault();

                    // Si no existe un médico asociado → lista vacía
                    if (idMedico == 0)
                    {
                        recetasQuery = Enumerable.Empty<recetas>().AsQueryable();
                    }
                    else
                    {
                        recetasQuery = db.recetas
                            .Include(r => r.consultas)
                            .Include(r => r.medicamentos)
                            .Where(r => r.consultas.IdMedico == idMedico);
                    }
                }
                // -------------------------
                // OTROS ROLES
                // -------------------------
                else
                {
                    recetasQuery = Enumerable.Empty<recetas>().AsQueryable();
                }

                // Ejecutar consulta
                var lista = recetasQuery.ToList();
                System.Diagnostics.Debug.WriteLine("Total recetas cargadas: " + lista.Count);

                return View(lista);
            }
            catch (Exception ex)
            {
                // PREPARAR INFORMACIÓN DEL ERROR PARA TU VISTA PERSONALIZADA
                var errorInfo = new HandleErrorInfo(ex, "Recetas", "Index");

                // REDIRCIONAR A LA VISTA ERROR
                return View("Error", errorInfo);
            }
        }


        // GET: Receta/Details/5
        [Authorize(Roles = "SuperAdmin, Administrador, Medico")]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            recetas receta = db.recetas
                .Include(r => r.consultas)
                .Include(r => r.medicamentos)
                .FirstOrDefault(r => r.IdReceta == id);

            if (receta == null)
                return HttpNotFound();

            return View(receta);
        }

        // GET: Receta/Create
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create()
        {
            var usuario = SessionHelper.CurrentUser;

            ViewBag.IdMedicamento = new SelectList(db.medicamentos, "IdMedicamento", "Nombre");

            if (usuario != null && User.IsInRole("Medico") && !User.IsInRole("SuperAdmin"))
            {
                var idMedico = db.medicos
                    .Where(m => m.IdUsuario == usuario.Id)
                    .Select(m => m.IdMedico)
                    .FirstOrDefault();

                var consultasPropias = db.consultas
                    .Where(c => c.IdMedico == idMedico)
                    .ToList()
                    .Select(c => new
                    {
                        c.IdConsulta,
                        Descripcion = "Consulta #" + c.IdConsulta + " - " + (c.Diagnostico ?? "")
                    })
                    .ToList();

                ViewBag.IdConsulta = new SelectList(consultasPropias, "IdConsulta", "Descripcion");
            }
            else
            {
                var todasConsultas = db.consultas
                    .Include(c => c.medicos)
                    .ToList()
                    .Select(c => new
                    {
                        c.IdConsulta,
                        Descripcion = "Consulta #" + c.IdConsulta + " - " + (c.medicos.Nombre ?? "")
                    })
                    .ToList();

                ViewBag.IdConsulta = new SelectList(todasConsultas, "IdConsulta", "Descripcion");
            }

            return View();
        }

        // POST: Receta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Create([Bind(Include = "IdReceta,IdConsulta,IdMedicamento,Cantidad")] recetas receta)
        {
            var usuario = SessionHelper.CurrentUser;

            if (usuario != null && User.IsInRole("Medico") && !User.IsInRole("SuperAdmin"))
            {
                var idMedico = db.medicos
                    .Where(m => m.IdUsuario == usuario.Id)
                    .Select(m => m.IdMedico)
                    .FirstOrDefault();

                bool consultaPropia = db.consultas.Any(c => c.IdConsulta == receta.IdConsulta && c.IdMedico == idMedico);
                if (!consultaPropia)
                {
                    ModelState.AddModelError("IdConsulta", "No puede crear recetas para consultas que no le pertenecen.");
                }
            }

            if (ModelState.IsValid)
            {
                db.recetas.Add(receta);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IdMedicamento = new SelectList(db.medicamentos, "IdMedicamento", "Nombre", receta.IdMedicamento);

            if (User.IsInRole("SuperAdmin"))
            {
                var todasConsultas = db.consultas
                    .Include(c => c.medicos)
                    .ToList()
                    .Select(c => new
                    {
                        c.IdConsulta,
                        Descripcion = "Consulta " + (c.medicos.Nombre ?? "") + " - " + (c.Diagnostico ?? "")
                    })
                    .ToList();

                ViewBag.IdConsulta = new SelectList(todasConsultas, "IdConsulta", "Descripcion", receta.IdConsulta);
            }
            else
            {
                var idMedico = db.medicos
                    .Where(m => m.IdUsuario == usuario.Id)
                    .Select(m => m.IdMedico)
                    .FirstOrDefault();

                var consultasPropias = db.consultas
                    .Where(c => c.IdMedico == idMedico)
                    .ToList()
                    .Select(c => new
                    {
                        c.IdConsulta,
                        Descripcion = "Consulta #" + c.IdConsulta + " - " + (c.Diagnostico ?? "")
                    })
                    .ToList();

                ViewBag.IdConsulta = new SelectList(consultasPropias, "IdConsulta", "Descripcion", receta.IdConsulta);
            }

            return View(receta);
        }

        // GET: Receta/Edit/5
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            recetas receta = db.recetas.Find(id);
            if (receta == null)
                return HttpNotFound();

            ViewBag.IdConsulta = new SelectList(db.consultas, "IdConsulta", "Diagnostico", receta.IdConsulta);
            ViewBag.IdMedicamento = new SelectList(db.medicamentos, "IdMedicamento", "Nombre", receta.IdMedicamento);
            return View(receta);
        }

        // POST: Receta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Medico")]
        public ActionResult Edit([Bind(Include = "IdReceta,IdConsulta,IdMedicamento,Cantidad")] recetas receta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(receta).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IdConsulta = new SelectList(db.consultas, "IdConsulta", "Diagnostico", receta.IdConsulta);
            ViewBag.IdMedicamento = new SelectList(db.medicamentos, "IdMedicamento", "Nombre", receta.IdMedicamento);
            return View(receta);
        }

        // GET: Receta/Delete/5
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            recetas receta = db.recetas
                .Include(r => r.consultas)
                .Include(r => r.medicamentos)
                .FirstOrDefault(r => r.IdReceta == id);

            if (receta == null)
                return HttpNotFound();

            return View(receta);
        }

        // POST: Receta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult DeleteConfirmed(int id)
        {
            recetas receta = db.recetas.Find(id);
            db.recetas.Remove(receta);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Receta/RecetasPaciente → Para pacientes logueados
        [Authorize(Roles = "Paciente")]
        public ActionResult RecetasPaciente()
        {
            var usuario = SessionHelper.CurrentUser;

            if (usuario == null)
                return RedirectToAction("Login", "Account");

            // Buscar el IdPaciente correspondiente al usuario actual
            var idPaciente = db.pacientes
                .Where(p => p.IdUsuario == usuario.Id)
                .Select(p => p.IdPaciente)
                .FirstOrDefault();

            if (idPaciente == 0)
            {
                TempData["ErrorMensaje"] = "No se encontró un perfil de paciente asociado a este usuario.";
                return RedirectToAction("Index", "Home");
            }

            // Obtener recetas relacionadas con las consultas del paciente actual
            var recetas = db.recetas
                .Include(r => r.consultas)
                .Include(r => r.medicamentos)
                .Where(r => r.consultas.IdPaciente == idPaciente)
                .AsNoTracking()
                .ToList();

            return View("RecetasPaciente", recetas);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
