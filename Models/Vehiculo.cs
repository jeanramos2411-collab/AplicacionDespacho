using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AplicacionDespacho.Models
{
    public class Vehiculo
    {
        public int VehiculoId { get; set; }
        public string Placa { get; set; }
        public int EmpresaId { get; set; }

        // AGREGAR ESTA PROPIEDAD    
        public string NombreEmpresa { get; set; }
    }
}
