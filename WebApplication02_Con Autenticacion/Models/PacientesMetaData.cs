using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebApplication02_Con_Autenticacion.Models
{
    [MetadataType(typeof(pacientesMetadata))]
    public partial class pacientes
    {
        public bool CedulaValidaEcuatoriana(int cedulaInt)
        {
            string cedula = cedulaInt.ToString().PadLeft(10, '0');

            if (string.IsNullOrWhiteSpace(cedula) || cedula.Length != 10 || !cedula.All(char.IsDigit))
                return false;

            int provincia = int.Parse(cedula.Substring(0, 2));
            int tercer = int.Parse(cedula.Substring(2, 1));
            int verificador = int.Parse(cedula.Substring(9, 1));

            if (provincia < 1 || provincia > 24 || tercer > 5)
                return false;

            int[] coef = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int suma = 0;

            for (int i = 0; i < 9; i++)
            {
                int valor = int.Parse(cedula[i].ToString()) * coef[i];
                if (valor >= 10) valor -= 9;
                suma += valor;
            }

            int resultado = (10 - (suma % 10)) % 10;
            return resultado == verificador;
        }
    }

    public class pacientesMetadata
    {
        [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
        [RegularExpression("^[A-Za-zÁÉÍÓÚáéíóúÑñ\\s]+$", ErrorMessage = "El nombre solo puede contener letras.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [Range(1, 9999999999, ErrorMessage = "La cédula debe tener hasta 10 dígitos.")]
        public int Cedula { get; set; }

        [Required(ErrorMessage = "La edad es obligatoria.")]
        [Range(1, 120, ErrorMessage = "La edad debe estar entre 1 y 120.")]
        public int Edad { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un género.")]
        public string Genero { get; set; }

        [Required(ErrorMessage = "La estatura es obligatoria.")]
        [Range(30, 250, ErrorMessage = "La estatura debe estar entre 30 y 250 cm.")]
        public int Estatura { get; set; }

        [Required(ErrorMessage = "El peso es obligatorio.")]
        [Range(1, 300, ErrorMessage = "El peso debe estar entre 1 y 300 kg.")]
        public double Peso { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una foto.")]
        public string Foto { get; set; }
    }
}
