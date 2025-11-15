using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebApplication02_Con_Autenticacion.Models
{
    [MetadataType(typeof(consultasMetadata))]
    public partial class consultas
    {
        public bool HorarioValido()
        {
            return HF > HI;
        }
        public bool FechaValida()
        {
            return FechaConsulta.Date <= DateTime.Now.Date;
        }
    }

    public class consultasMetadata
    {
        [Required(ErrorMessage = "Debe seleccionar un médico.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un médico válido.")]
        [Display(Name = "Médico")]
        public int IdMedico { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un paciente.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un paciente válido.")]
        [Display(Name = "Paciente")]
        public int IdPaciente { get; set; }

        [Required(ErrorMessage = "La fecha de la consulta es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Consulta")]
        public DateTime FechaConsulta { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora Inicio")]
        public TimeSpan HI { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora Fin")]
        public TimeSpan HF { get; set; }

        [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
        [StringLength(500, ErrorMessage = "El diagnóstico no puede superar los 500 caracteres.")]
        [Display(Name = "Diagnóstico")]
        public string Diagnostico { get; set; }
    }
}
