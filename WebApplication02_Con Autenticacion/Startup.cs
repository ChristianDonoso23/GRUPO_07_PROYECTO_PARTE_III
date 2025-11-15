using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebApplication02_Con_Autenticacion.Startup))]
namespace WebApplication02_Con_Autenticacion
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
