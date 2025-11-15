using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebApplication02_Con_Autenticacion.Models
{
    [MetadataType(typeof(MedicoMetadata))]
    public partial class medicos
    {
    }

    public class MedicoMetadata
    {
        [Required(ErrorMessage = "El nombre del médico es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        [RegularExpression("^[A-Za-zÁÉÍÓÚáéíóúÑñ\\s]+$", ErrorMessage = "El nombre solo puede contener letras.")]
        [Display(Name = "Nombre Completo")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una especialidad.")]
        [Display(Name = "Especialidad")]
        public int IdEspecialidad { get; set; }

        [Display(Name = "Usuario Asociado")]
        public string IdUsuario { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una foto para el médico.")]
        [Display(Name = "Foto del Médico")]
        public string Foto { get; set; }
    }
}
