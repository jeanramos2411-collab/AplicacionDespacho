using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AplicacionDespacho.Models
{
    public class PesoEmbalaje
        {
        public int PesoEmbalajeId { get; set; }
        public string NombreEmbalaje { get; set; }
        public decimal PesoUnitario { get; set; }
        public int? TotalCajasFichaTecnica { get; set; } // NUEVO CAMPO  
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public bool Activo { get; set; }
        }
}