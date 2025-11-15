using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication02_Con_Autenticacion.Models.ViewModels
{
    public class UserRoleViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Roles { get; set; }
    }
}
