using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebApplication02_Con_Autenticacion.Models;

namespace WebApplication02_Con_Autenticacion
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ApplicationDbContext db = new ApplicationDbContext();

            CrearRolesSistema(db);
            CrearUsuariosSistema(db);
            AsignarRolUsuario(db);
        }

        /* Crear roles con IDs fijos */
        private void CrearRolesSistema(ApplicationDbContext db)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

            var roles = new[]
            {
        new IdentityRole { Id = "1", Name = "Administrador" },
        new IdentityRole { Id = "2", Name = "Medico" },
        new IdentityRole { Id = "3", Name = "Paciente" },
        new IdentityRole { Id = "4", Name = "SuperAdmin" }
    };

            foreach (var rol in roles)
            {
                /* Validación 1: ¿Existe este rol por ID? */
                var existePorId = db.Roles.Any(r => r.Id == rol.Id);

                /* Validación 2: ¿Existe este rol por nombre */
                var existePorNombre = roleManager.RoleExists(rol.Name);

                if (existePorId || existePorNombre)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[INFO] Rol '{rol.Name}' ya existe (ID={rol.Id}). No se crea de nuevo.");
                    continue;
                }

                /* Si no existe ni por ID ni por Nombre → ahora sí lo creamos */
                db.Roles.Add(rol);
                System.Diagnostics.Debug.WriteLine(
                    $"[CREADO] Rol '{rol.Name}' registrado con ID={rol.Id}");
            }

            db.SaveChanges();
        }


        /* Crear usuarios base */
        private void CrearUsuariosSistema(ApplicationDbContext db)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            userManager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 1,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false
            };

            CrearUsuarioSistema(userManager, "SuperAdmin", "superadmin@veris.com");
            CrearUsuarioSistema(userManager, "ADM", "admin@veris.com");
        }

        /* Auxiliar para crear usuario con pass por defecto */
        private void CrearUsuarioSistema(UserManager<ApplicationUser> userManager, string username, string email)
        {
            var usuario = userManager.FindByName(username);
            if (usuario == null)
            {
                var nuevoUsuario = new ApplicationUser
                {
                    UserName = username,
                    Email = email
                };

                var resultado = userManager.Create(nuevoUsuario, "123");

                if (resultado.Succeeded)
                    System.Diagnostics.Debug.WriteLine($"Usuario '{username}' creado (pass: 123)");
                else
                    System.Diagnostics.Debug.WriteLine($"Error creando '{username}': {string.Join(", ", resultado.Errors)}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Usuario '{username}' ya existe");
            }
        }

        /* 3️⃣ Asignar roles a los usuarios */
        private void AsignarRolUsuario(ApplicationDbContext db)
        {
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            var superadmin = userManager.FindByName("SuperAdmin");
            if (superadmin != null)
            {
                AsignarRoles(userManager, superadmin.Id, "SuperAdmin", "Administrador", "Medico", "Paciente");
            }

            var administrador = userManager.FindByName("ADM");
            if (administrador != null)
            {
                AsignarRoles(userManager, administrador.Id, "Administrador");
            }
        }

        /* Auxiliar asignar múltiples roles */
        private void AsignarRoles(UserManager<ApplicationUser> userManager, string userId, params string[] roles)
        {
            foreach (var rol in roles)
            {
                if (!userManager.IsInRole(userId, rol))
                {
                    userManager.AddToRole(userId, rol);
                    System.Diagnostics.Debug.WriteLine($"Rol '{rol}' asignado al usuario ID={userId}");
                }
            }
        }
    }
}
