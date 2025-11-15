using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Linq;
using System.Web.Mvc;
using WebApplication02_Con_Autenticacion.Models;
using WebApplication02_Con_Autenticacion.Models.ViewModels;

namespace WebApplication02_Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class RolController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            var users = db.Users.ToList().Select(u => new UserRoleViewModel
            {
                Id = u.Id,
                Email = u.Email,
                Roles = string.Join(", ", userManager.GetRoles(u.Id))
            }).ToList();

            return View(users);
        }

        public ActionResult AsignarRol(string id)
        {
            if (id == null)
                return HttpNotFound();

            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            ViewBag.Roles = new SelectList(db.Roles, "Name", "Name");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AsignarRol(string id, string rolSeleccionado)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = db.Users.Find(id);

            if (user == null)
                return HttpNotFound();

            /* Eliminar roles previos */
            var rolesActuales = userManager.GetRoles(user.Id).ToArray();
            if (rolesActuales.Any())
                userManager.RemoveFromRoles(user.Id, rolesActuales);

            /* Asignar nuevo rol */
            userManager.AddToRole(user.Id, rolSeleccionado);

            TempData["Mensaje"] = $"Rol '{rolSeleccionado}' asignado a {user.Email}";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuitarRol(string id)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = db.Users.Find(id);

            if (user == null)
                return HttpNotFound();

            /* Obtener todos los roles del usuario */
            var rolesActuales = userManager.GetRoles(user.Id).ToArray();

            /* Si tiene alguno, se eliminan */
            if (rolesActuales.Any())
            {
                userManager.RemoveFromRoles(user.Id, rolesActuales);
                TempData["Mensaje"] = $"Se eliminaron todos los roles del usuario {user.Email}.";
            }
            else
            {
                TempData["Mensaje"] = $"El usuario {user.Email} no tenía roles asignados.";
            }

            return RedirectToAction("Index");
        }

    }
}
