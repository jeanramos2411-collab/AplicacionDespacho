// Services/Export/BaseViajeExportService.cs  
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AplicacionDespacho.Models.Export;

namespace AplicacionDespacho.Services.Export
{
    public abstract class BaseViajeExportService : IExportService<ViajeReportData>
    {
        protected abstract ExportFormat SupportedFormat { get; }

        public virtual bool SupportsFormat(ExportFormat format)
        {
            return format == SupportedFormat;
        }

        public abstract string GetDefaultFileExtension(ExportFormat format);

        public async Task<ExportResult> ExportAsync(IEnumerable<ViajeReportData> data, ExportOptions options)
        {
            try
            {
                ValidateOptions(options);

                var reportData = data.FirstOrDefault();
                if (reportData == null)
                {
                    return ExportResult.CreateError("No hay datos para exportar.");
                }

                await ValidateDataAsync(reportData);

                var result = await ExportInternalAsync(reportData, options);

                return result;
            }
            catch (Exception ex)
            {
                return ExportResult.CreateError($"Error durante la exportación: {ex.Message}", ex);
            }
        }

        protected virtual void ValidateOptions(ExportOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.FilePath))
                throw new ArgumentException("La ruta del archivo es requerida.", nameof(options.FilePath));

            if (!SupportsFormat(options.Format))
                throw new NotSupportedException($"El formato {options.Format} no está soportado.");
        }

        protected virtual Task ValidateDataAsync(ViajeReportData data)
        {
            if (data.Viaje == null)
                throw new ArgumentException("Los datos del viaje son requeridos.");

            return Task.CompletedTask;
        }

        protected abstract Task<ExportResult> ExportInternalAsync(ViajeReportData data, ExportOptions options);

        protected virtual string GenerateDefaultFileName(ViajeReportData data, ExportFormat format)
        {
            var extension = GetDefaultFileExtension(format);
            return $"Reporte_Viaje_{data.Viaje.NumeroViaje}_{data.Viaje.Fecha:yyyyMMdd}.{extension}";
        }
    }
}