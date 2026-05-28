// Services/Export/ViajeCsvExportService.cs  
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AplicacionDespacho.Models.Export;

namespace AplicacionDespacho.Services.Export
{
    public class ViajeCsvExportService : BaseViajeExportService
    {
        protected override ExportFormat SupportedFormat => ExportFormat.CSV;

        public override string GetDefaultFileExtension(ExportFormat format)
        {
            return format == ExportFormat.CSV ? "csv" : throw new NotSupportedException();
        }

        protected override async Task<ExportResult> ExportInternalAsync(ViajeReportData data, ExportOptions options)
        {
            var csv = new StringBuilder();

            // Información del viaje  
            csv.AppendLine("INFORMACIÓN DEL VIAJE");
            csv.AppendLine($"Fecha,{data.Viaje.Fecha:dd/MM/yyyy}");
            csv.AppendLine($"N° Viaje,{data.Viaje.NumeroViaje}");
            csv.AppendLine($"N° Guía,{data.Viaje.NumeroGuia}");
            csv.AppendLine($"Responsable,{data.Viaje.Responsable}");
            csv.AppendLine($"Empresa,{data.Viaje.NombreEmpresa}");
            csv.AppendLine($"Conductor,{data.Viaje.NombreConductor}");
            csv.AppendLine($"Placa Vehículo,{data.Viaje.PlacaVehiculo ?? "N/A"}");
            csv.AppendLine($"Punto Partida,{data.Viaje.PuntoPartida ?? "N/A"}");
            csv.AppendLine($"Punto Llegada,{data.Viaje.PuntoLlegada ?? "N/A"}");
            csv.AppendLine();

            // Encabezados de pallets  
            csv.AppendLine("PALLETS DEL VIAJE");
            csv.AppendLine("Pallet,Variedad,Calibre,Embalaje,N° Cajas,Peso Unit. (kg),Peso Total (kg),Modificado");

            // Datos de pallets  
            foreach (var pallet in data.Pallets)
            {
                csv.AppendLine($"{pallet.NumeroPallet},{pallet.Variedad},{pallet.Calibre},{pallet.Embalaje}," +
                              $"{pallet.NumeroDeCajas},{pallet.PesoUnitario:F3},{pallet.PesoTotal:F3}," +
                              $"{(pallet.Modificado ? "Sí" : "No")}");
            }

            // Totales  
            csv.AppendLine();
            csv.AppendLine("RESUMEN DE TOTALES");
            csv.AppendLine($"Total Pallets,{data.Pallets.Count}");
            csv.AppendLine($"Total Cajas,{data.Pallets.Sum(p => p.NumeroDeCajas)}");
            csv.AppendLine($"Peso Total (kg),{data.Pallets.Sum(p => p.PesoTotal):F3}");

            await File.WriteAllTextAsync(options.FilePath, csv.ToString(), Encoding.UTF8);

            return ExportResult.CreateSuccess(options.FilePath,
                $"Reporte CSV exportado exitosamente con {data.Pallets.Count} pallets.");
        }
    }
}