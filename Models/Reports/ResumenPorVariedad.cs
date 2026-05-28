// Models/Reports/ResumenPorVariedad.cs  
using System.Collections.Generic;

namespace AplicacionDespacho.Models.Reports
{
    public class ResumenPorVariedad
    {
        public string Variedad { get; set; }
        public int TotalCajas { get; set; }
        public decimal TotalKilos { get; set; }
        public int TotalPallets { get; set; }
        // Agregar después de las propiedades existentes  
        public int TotalCT { get; set; }
        public int TotalEN { get; set; }
        public List<ResumenVariedadEmbalaje> DetallesPorEmbalaje { get; set; }
        // AGREGAR ESTAS PROPIEDADES:  
        public string VariedadParaReporte => Variedad; // Para compatibilidad  
        public int CajasParaReporte => TotalCajas; // Para compatibilidad  


        public ResumenPorVariedad()
        {
            DetallesPorEmbalaje = new List<ResumenVariedadEmbalaje>();
        }

    }
}