using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication02_Con_Autenticacion.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using WebApplication02_Con_Autenticacion.Helpers;

namespace WebApplication02_Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperAdmin, Administrador, Paciente")]
    public class PacienteController : Controller
    {
        private ProyectoVeris_Context db = new ProyectoVeris_Context();

        // GET: Paciente
        public ActionResult Index()
        {
            var usuario = SessionHelper.CurrentUser;
            if (usuario == null)
                return RedirectToAction("Login", "Account");

            IQueryable<pacientes> pacientesQuery;

            if (User.IsInRole("SuperAdmin") || User.IsInRole("Administrador"))
            {
                pacientesQuery = db.pacientes.Include(p => p.AspNetUsers);
            }
            else if (User.IsInRole("Paciente"))
            {
                pacientesQuery = db.pacientes
                    .Include(p => p.AspNetUsers)
                    .Where(p => p.IdUsuario == usuario.Id);
            }
            else
            {
                pacientesQuery = Enumerable.Empty<pacientes>().AsQueryable();
            }

            var lista = pacientesQuery.ToList();
            System.Diagnostics.Debug.WriteLine("Pacientes cargados: " + lista.Count);
            return View(lista);
        }

        // GET: Paciente/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usuario = SessionHelper.CurrentUser;
            var paciente = db.pacientes.Find(id);

            if (paciente == null)
                return HttpNotFound();

            if (User.IsInRole("Paciente") && !User.IsInRole("SuperAdmin") && paciente.IdUsuario != usuario.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Acceso denegado");

            return View(paciente);
        }

        // GET: Paciente/Create
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult Create()
        {
            var identityDb = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

            var todosUsuarios = userManager.Users.ToList();

            var usuariosConRolPaciente = todosUsuarios
                .Where(u => userManager.IsInRole(u.Id, "Paciente"))
                .ToList();

            var usuariosConFicha = db.pacientes.Select(p => p.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolPaciente
                .Where(u => !usuariosConFicha.Contains(u.Id))
                .ToList();

            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email");

            string path = Server.MapPath("~/Imagenes/Usuarios");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos);

            return View();
        }

        // POST: Paciente/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult Create([Bind(Include = "IdPaciente,IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] pacientes paciente)
        {
            string path = Server.MapPath("~/Imagenes/Usuarios");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, paciente.Foto);

            var identityDb = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

            var todosUsuarios = userManager.Users.ToList();
            var usuariosConRolPaciente = todosUsuarios
                .Where(u => userManager.IsInRole(u.Id, "Paciente"))
                .ToList();
            var usuariosConFicha = db.pacientes.Select(p => p.IdUsuario).ToList();
            var usuariosDisponibles = usuariosConRolPaciente
                .Where(u => !usuariosConFicha.Contains(u.Id))
                .ToList();
            ViewBag.IdUsuario = new SelectList(usuariosDisponibles, "Id", "Email", paciente.IdUsuario);

            if (!paciente.CedulaValidaEcuatoriana(paciente.Cedula))
            {
                ModelState.AddModelError("Cedula", "Cédula ecuatoriana inválida. Revise el número ingresado.");
                return View(paciente);
            }

            if (!ModelState.IsValid)
                return View(paciente);

            if (string.IsNullOrEmpty(paciente.IdUsuario))
            {
                if (string.IsNullOrWhiteSpace(paciente.Nombre))
                {
                    ModelState.AddModelError("", "Debe ingresar un nombre para generar el usuario automáticamente.");
                    return View(paciente);
                }

                var partes = paciente.Nombre.Trim().Split(' ');
                string primerNombre = partes.Length > 0 ? partes[0] : paciente.Nombre;
                string apellido = partes.Length > 1 ? partes[partes.Length - 1] : "paciente";

                string Sanitize(string s) =>
                    new string(s.Normalize(System.Text.NormalizationForm.FormD)
                        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                        .ToArray())
                    .Replace(" ", "")
                    .ToLowerInvariant();

                string baseUsuario = (Sanitize(primerNombre).Length >= 2
                    ? Sanitize(primerNombre).Substring(0, 2)
                    : Sanitize(primerNombre)) + Sanitize(apellido);

                string email = baseUsuario + "@hotmail.com";
                int contador = 1;
                string emailFinal = email;
                string usernameFinal = baseUsuario;

                while (userManager.FindByEmail(emailFinal) != null || userManager.FindByName(usernameFinal) != null)
                {
                    emailFinal = $"{baseUsuario}{contador}@hotmail.com";
                    usernameFinal = $"{baseUsuario}{contador}";
                    contador++;
                }

                userManager.PasswordValidator = new PasswordValidator
                {
                    RequiredLength = 1,
                    RequireDigit = false,
                    RequireLowercase = false,
                    RequireUppercase = false,
                    RequireNonLetterOrDigit = false
                };

                var nuevoUsuario = new ApplicationUser
                {
                    UserName = usernameFinal,
                    Email = emailFinal,
                    EmailConfirmed = true
                };

                string password = "123";
                var result = userManager.Create(nuevoUsuario, password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError("", error);
                    return View(paciente);
                }

                if (!userManager.IsInRole(nuevoUsuario.Id, "Paciente"))
                    userManager.AddToRole(nuevoUsuario.Id, "Paciente");

                paciente.IdUsuario = nuevoUsuario.Id;
            }

            db.pacientes.Add(paciente);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Paciente/Edit/5
        [Authorize(Roles = "SuperAdmin, Administrador, Paciente")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var usuario = SessionHelper.CurrentUser;
            var paciente = db.pacientes.Find(id);

            if (paciente == null)
                return HttpNotFound();

            if (User.IsInRole("Paciente") && !User.IsInRole("SuperAdmin") && paciente.IdUsuario != usuario.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Acceso denegado");

            string path = Server.MapPath("~/Imagenes/Usuarios");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, paciente.Foto);

            ViewBag.IdUsuario = new SelectList(db.AspNetUsers.Where(u => u.Id == paciente.IdUsuario), "Id", "Email", paciente.IdUsuario);

            return View(paciente);
        }

        // POST: Paciente/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Administrador, Paciente")]
        public ActionResult Edit([Bind(Include = "IdPaciente,IdUsuario,Nombre,Cedula,Edad,Genero,Estatura,Peso,Foto")] pacientes paciente)
        {
            string path = Server.MapPath("~/Imagenes/Usuarios");
            var archivos = System.IO.Directory.GetFiles(path)
                .Select(f => System.IO.Path.GetFileName(f))
                .ToList();
            ViewBag.Fotos = new SelectList(archivos, paciente.Foto);

            var usuario = SessionHelper.CurrentUser;

            if (User.IsInRole("Paciente") && !User.IsInRole("SuperAdmin") && paciente.IdUsuario != usuario.Id)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Acceso denegado");

            if (!paciente.CedulaValidaEcuatoriana(paciente.Cedula))
            {
                ModelState.AddModelError("Cedula", "Cédula ecuatoriana inválida. Revise el número ingresado.");
                return View(paciente);
            }

            if (!ModelState.IsValid)
                return View(paciente);

            db.Entry(paciente).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Paciente/Delete/5
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var paciente = db.pacientes.Find(id);
            if (paciente == null)
                return HttpNotFound();

            return View(paciente);
        }

        // POST: Paciente/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin, Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            var paciente = db.pacientes.Find(id);
            if (paciente == null)
                return HttpNotFound();

            string idUsuario = paciente.IdUsuario;

            try
            {
                db.pacientes.Remove(paciente);
                db.SaveChanges();

                if (!string.IsNullOrEmpty(idUsuario))
                {
                    var identityDb = new ApplicationDbContext();
                    var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identityDb));

                    var usuario = userManager.FindById(idUsuario);
                    if (usuario != null)
                    {
                        var roles = userManager.GetRoles(usuario.Id);
                        foreach (var rol in roles)
                            userManager.RemoveFromRole(usuario.Id, rol);

                        userManager.Delete(usuario);
                    }
                }

                TempData["ExitoMensaje"] = "Paciente eliminado correctamente.";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                TempData["ErrorMensaje"] = "No se puede eliminar el paciente porque tiene consultas asignadas.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMensaje"] = "Ocurrió un error al intentar eliminar el paciente. Detalle: " + ex.Message;
            }

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
