// Services/Export/ExportServiceFactory.cs  
using System;
using System.Collections.Generic;
using AplicacionDespacho.Models.Export;

namespace AplicacionDespacho.Services.Export
{
    public class ExportServiceFactory
    {
        private readonly Dictionary<ExportFormat, Func<IExportService<ViajeReportData>>> _viajeExporters;

        public ExportServiceFactory()
        {
            _viajeExporters = new Dictionary<ExportFormat, Func<IExportService<ViajeReportData>>>
            {
                { ExportFormat.Excel, () => new ViajeExcelExportService() },
                { ExportFormat.CSV, () => new ViajeCsvExportService() },  
                // Preparado para futuras implementaciones  
                // { ExportFormat.PDF, () => new ViajePdfExportService() },  
                // { ExportFormat.JSON, () => new ViajeJsonExportService() }  
            };
        }

        public IExportService<ViajeReportData> CreateViajeExporter(ExportFormat format)
        {
            if (_viajeExporters.TryGetValue(format, out var factory))
            {
                return factory();
            }

            throw new NotSupportedException($"El formato {format} no está soportado para reportes de viaje.");
        }

        public IEnumerable<ExportFormat> GetSupportedFormats()
        {
            return _viajeExporters.Keys;
        }
    }
}