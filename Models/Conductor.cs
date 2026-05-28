using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AplicacionDespacho.Models
{
    public class Conductor
    {
        public int ConductorId { get; set; }
        public string NombreConductor { get; set; }
        public int EmpresaId { get; set; }

        // AGREGAR ESTA PROPIEDAD    
        public string NombreEmpresa { get; set; }
    }
}
