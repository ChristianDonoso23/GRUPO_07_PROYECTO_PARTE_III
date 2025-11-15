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
    public class MedicamentoController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // GET: Medicamento
        public ActionResult Index()
        {
            return View(db.medicamentos.ToList());
        }

        // GET: Medicamento/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            medicamentos medicamento = db.medicamentos.Find(id);
            if (medicamento == null)
                return HttpNotFound();

            return View(medicamento);
        }

        // GET: Medicamento/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Medicamento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "IdMedicamento,Nombre,Tipo")] medicamentos medicamento)
        {
            if (ModelState.IsValid)
            {
                db.medicamentos.Add(medicamento);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(medicamento);
        }

        // GET: Medicamento/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            medicamentos medicamento = db.medicamentos.Find(id);
            if (medicamento == null)
                return HttpNotFound();

            return View(medicamento);
        }

        // POST: Medicamento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IdMedicamento,Nombre,Tipo")] medicamentos medicamento)
        {
            if (ModelState.IsValid)
            {
                db.Entry(medicamento).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(medicamento);
        }

        // GET: Medicamento/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            medicamentos medicamento = db.medicamentos.Find(id);
            if (medicamento == null)
                return HttpNotFound();

            return View(medicamento);
        }

        // POST: Medicamento/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            medicamentos medicamento = db.medicamentos.Find(id);
            db.medicamentos.Remove(medicamento);
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
