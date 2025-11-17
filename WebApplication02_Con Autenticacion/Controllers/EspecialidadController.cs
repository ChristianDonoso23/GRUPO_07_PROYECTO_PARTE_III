using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication02_Con_Autenticacion.Models;

namespace WebApplication02_Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperAdmin, Administrador")]
    public class EspecialidadController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // GET: Especialidad
        public ActionResult Index()
        {
            try
            {
                var lista = db.especialidades.ToList();
                return View(lista);
            }
            catch (Exception ex)
            {
                var errorInfo = new HandleErrorInfo(ex, "Especialidades", "Index");
                return View("~/Views/Shared/Error.cshtml", errorInfo);
            }
        }


        // GET: Especialidad/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            especialidades especialidad = db.especialidades.Find(id);
            if (especialidad == null)
                return HttpNotFound();

            return View(especialidad);
        }

        // GET: Especialidad/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Especialidad/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "IdEspecialidad,Descripcion,Dias,Franja_HI,Franja_HF")] especialidades especialidad)
        {
            if (ModelState.IsValid)
            {
                db.especialidades.Add(especialidad);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(especialidad);
        }

        // GET: Especialidad/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            especialidades especialidad = db.especialidades.Find(id);
            if (especialidad == null)
                return HttpNotFound();

            return View(especialidad);
        }

        // POST: Especialidad/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IdEspecialidad,Descripcion,Dias,Franja_HI,Franja_HF")] especialidades especialidad)
        {
            if (ModelState.IsValid)
            {
                db.Entry(especialidad).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(especialidad);
        }

        // GET: Especialidad/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            especialidades especialidad = db.especialidades.Find(id);
            if (especialidad == null)
                return HttpNotFound();

            return View(especialidad);
        }

        // POST: Especialidad/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            especialidades especialidad = db.especialidades.Find(id);
            db.especialidades.Remove(especialidad);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
