// Models/Reports/ReporteGeneralFiltros.cs  
using System;

namespace AplicacionDespacho.Models.Reports
{
    public class ReporteGeneralFiltros
    {
        public DateTime? FechaEnvioDesde { get; set; }
        public DateTime? FechaEnvioHasta { get; set; }
        public DateTime? FechaViajeDesde { get; set; }
        public DateTime? FechaViajeHasta { get; set; }
        public string Variedad { get; set; }
        public string Calibre { get; set; }
        public string Embalaje { get; set; }
        public int? EmpresaId { get; set; }
        public string NumeroGuia { get; set; }
        public string Responsable { get; set; }
        public string EstadoViaje { get; set; }
        public string NumeroPallet { get; set; }
    }
}