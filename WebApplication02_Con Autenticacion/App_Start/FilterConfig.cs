using System.Web;
using System.Web.Mvc;

namespace WebApplication02_Con_Autenticacion
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new Filters.Filtro_Inicio_Sesion());
        }
    }
}
