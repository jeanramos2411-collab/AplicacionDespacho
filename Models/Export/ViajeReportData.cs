// Models/Export/ViajeReportData.cs  
using System;
using System.Collections.Generic;

namespace AplicacionDespacho.Models.Export
{
    public class ViajeReportData
    {
        public Viaje Viaje { get; set; }
        public List<InformacionPallet> Pallets { get; set; }
        public ReportMetadata Metadata { get; set; }

        public ViajeReportData()
        {
            Pallets = new List<InformacionPallet>();
            Metadata = new ReportMetadata();
        }
    }

    public class ReportMetadata
    {
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = Environment.UserName;
        public string Version { get; set; } = "1.0";
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }
}