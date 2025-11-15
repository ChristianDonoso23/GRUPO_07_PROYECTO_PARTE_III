using WebApplication02_Con_Autenticacion.Models;
using System.Web;

namespace WebApplication02_Con_Autenticacion.Helpers
{
    // Clase estática que centraliza el acceso a la sesión del usuario autenticado.
    // Permite recuperar fácilmente información del usuario actual desde cualquier parte del sistema (controladores, vistas, etc.).
    public static class SessionHelper
    {
        // Propiedad estática que obtiene el usuario actual almacenado en la sesión.
        // Se espera que en la sesión de ASP.NET se haya guardado un objeto de tipo "ApplicationUser" bajo la clave "User".
        public static ApplicationUser CurrentUser
        {
            get
            {
                // HttpContext.Current → obtiene el contexto HTTP actual (la solicitud activa).
                // Session["User"] → accede a la variable de sesión llamada "User" (donde se guarda el usuario al iniciar sesión).
                // Si no existe la sesión o no hay usuario, devuelve null.
                return HttpContext.Current?.Session?["User"] as ApplicationUser;
            }
        }

        // Propiedad auxiliar que devuelve el nombre de usuario del usuario en sesión.
        // Si no hay sesión activa, devuelve una cadena vacía.
        public static string CurrentUserName => CurrentUser?.UserName ?? "";

        // Propiedad auxiliar que devuelve el correo electrónico del usuario en sesión.
        // Si no hay sesión activa, también devuelve una cadena vacía.
        public static string CurrentEmail => CurrentUser?.Email ?? "";
    }
}
