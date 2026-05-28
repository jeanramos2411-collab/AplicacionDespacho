// Services/Export/ViajeExcelExportService.cs    
using AplicacionDespacho.Models;
using AplicacionDespacho.Models.Export;
using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml.Style;

namespace AplicacionDespacho.Services.Export
{
    public class ViajeExcelExportService : BaseViajeExportService
    {
        protected override ExportFormat SupportedFormat => ExportFormat.Excel;

        public override string GetDefaultFileExtension(ExportFormat format)
        {
            return format == ExportFormat.Excel ? "xlsx" : throw new NotSupportedException();
        }

        protected override async Task<ExportResult> ExportInternalAsync(ViajeReportData data, ExportOptions options)
        {
            // Configurar EPPlus para uso no comercial    
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Reporte de Viaje");

                await Task.Run(() => BuildWorksheet(worksheet, data, options));

                var fileInfo = new FileInfo(options.FilePath);
                await package.SaveAsAsync(fileInfo);

                return ExportResult.CreateSuccess(options.FilePath,
                    $"Reporte exportado exitosamente con {data.Pallets.Count} pallets.");
            }
        }

        private void BuildWorksheet(ExcelWorksheet worksheet, ViajeReportData data, ExportOptions options)
        {
            int currentRow = 1;

            // Título del reporte    
            currentRow = AddTitle(worksheet, currentRow, options.Title ?? "REPORTE DETALLADO DE VIAJE");
            currentRow += 2;

            // Información del viaje    
            currentRow = AddViajeInfo(worksheet, currentRow, data.Viaje);
            currentRow += 2;

            // Información de pallets    
            currentRow = AddPalletsSection(worksheet, currentRow, data.Pallets);
            currentRow += 2;

            // Totales    
            currentRow = AddTotalsSection(worksheet, currentRow, data.Pallets);
            currentRow += 2;

            // Metadata    
            AddMetadataSection(worksheet, currentRow, data.Metadata);

            // Ajustar columnas    
            worksheet.Cells.AutoFitColumns();
        }

        private int AddTitle(ExcelWorksheet worksheet, int startRow, string title)
        {
            worksheet.Cells[startRow, 1].Value = title;
            worksheet.Cells[startRow, 1, startRow, 8].Merge = true;
            worksheet.Cells[startRow, 1].Style.Font.Size = 16;
            worksheet.Cells[startRow, 1].Style.Font.Bold = true;
            worksheet.Cells[startRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            return startRow + 1;
        }

        private int AddViajeInfo(ExcelWorksheet worksheet, int startRow, Viaje viaje)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "INFORMACIÓN DEL VIAJE";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            // Fila 1: Fecha y N° Viaje    
            worksheet.Cells[row, 1].Value = "Fecha:";
            worksheet.Cells[row, 2].Value = viaje.Fecha.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 3].Value = "N° Viaje:";
            worksheet.Cells[row, 4].Value = viaje.NumeroViaje;
            row++;

            // Fila 2: N° Guía y Responsable    
            worksheet.Cells[row, 1].Value = "N° Guía:";
            worksheet.Cells[row, 2].Value = viaje.NumeroGuia;
            worksheet.Cells[row, 3].Value = "Responsable:";
            worksheet.Cells[row, 4].Value = viaje.Responsable;
            row++;

            // Fila 3: Empresa y Conductor    
            worksheet.Cells[row, 1].Value = "Empresa:";
            worksheet.Cells[row, 2].Value = viaje.NombreEmpresa;
            worksheet.Cells[row, 3].Value = "Conductor:";
            worksheet.Cells[row, 4].Value = viaje.NombreConductor;
            row++;

            // Fila 4: Placa y Punto Partida    
            worksheet.Cells[row, 1].Value = "Placa Vehículo:";
            worksheet.Cells[row, 2].Value = viaje.PlacaVehiculo ?? "N/A";
            worksheet.Cells[row, 3].Value = "Punto Partida:";
            worksheet.Cells[row, 4].Value = viaje.PuntoPartida ?? "N/A";
            row++;

            // Fila 5: Punto Llegada    
            worksheet.Cells[row, 1].Value = "Punto Llegada:";
            worksheet.Cells[row, 2].Value = viaje.PuntoLlegada ?? "N/A";
            row++;

            return row;
        }

        private int AddPalletsSection(ExcelWorksheet worksheet, int startRow, System.Collections.Generic.List<InformacionPallet> pallets)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "PALLETS DEL VIAJE";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            // Encabezados    
            string[] headers = { "Pallet", "Variedad", "Calibre", "Embalaje", "N° Cajas", "Peso Unit. (kg)", "Peso Total (kg)", "Modificado" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[row, i + 1].Value = headers[i];
            }

            // Formatear encabezados - CORREGIDO para EPPlus v7  
            using (var range = worksheet.Cells[row, 1, row, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
            row++;

            // Datos de pallets    
            foreach (var pallet in pallets)
            {
                worksheet.Cells[row, 1].Value = pallet.NumeroPallet;
                worksheet.Cells[row, 2].Value = pallet.VariedadParaReporte;
                worksheet.Cells[row, 3].Value = pallet.Calibre;
                worksheet.Cells[row, 4].Value = pallet.Embalaje;
                worksheet.Cells[row, 5].Value = pallet.TotalCajasDisplay;
                worksheet.Cells[row, 6].Value = pallet.PesoUnitario;
                worksheet.Cells[row, 7].Value = pallet.PesoTotal;
                worksheet.Cells[row, 8].Value = pallet.Modificado ? "Sí" : "No";

                // Formatear números    
                worksheet.Cells[row, 6].Style.Numberformat.Format = "0.000";
                worksheet.Cells[row, 7].Style.Numberformat.Format = "0.000";

                row++;
            }

            return row;
        }

        private int AddTotalsSection(ExcelWorksheet worksheet, int startRow, System.Collections.Generic.List<InformacionPallet> pallets)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "RESUMEN DE TOTALES";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            // Totales generales  
            worksheet.Cells[row, 1].Value = "Total Pallets:";
            worksheet.Cells[row, 2].Value = pallets.Count;
            row++;

            worksheet.Cells[row, 1].Value = "Total Cajas:";
            worksheet.Cells[row, 2].Value = pallets.Sum(p => p.CajasParaReporte);
            row++;

            worksheet.Cells[row, 1].Value = "Peso Total (kg):";
            worksheet.Cells[row, 2].Value = pallets.Sum(p => p.PesoTotal);
            worksheet.Cells[row, 2].Style.Numberformat.Format = "0.000";
            row++;

            // ESTRATEGIA CONDICIONAL: Detectar presencia de CT/EN  
            bool tieneCTEN = pallets.Any(p => p.EsCT || p.EsEN);

            if (!tieneCTEN)
            {
                // CASO 1: Solo PC/PH - Clasificación simple  
                worksheet.Cells[row, 1].Value = "CLASIFICACIÓN PC/PH";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                var totalPC = pallets.Count(p => p.EsPC);
                var totalPH = pallets.Count(p => p.EsPH);

                worksheet.Cells[row, 1].Value = "Pallets PC:";
                worksheet.Cells[row, 2].Value = totalPC;
                row++;

                worksheet.Cells[row, 1].Value = "Pallets PH:";
                worksheet.Cells[row, 2].Value = totalPH;
                row++;

                // Formatear sección PC/PH  
                using (var range = worksheet.Cells[row - 3, 1, row - 1, 2])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }
            }
            else
            {
                // CASO 2: Hay CT/EN - Clasificación completa separada  

                // Sección PC/PH  
                worksheet.Cells[row, 1].Value = "CLASIFICACIÓN PC/PH";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                var totalPC = pallets.Count(p => p.EsPC);
                var totalPH = pallets.Count(p => p.EsPH);

                worksheet.Cells[row, 1].Value = "Pallets PC:";
                worksheet.Cells[row, 2].Value = totalPC;
                row++;

                worksheet.Cells[row, 1].Value = "Pallets PH:";
                worksheet.Cells[row, 2].Value = totalPH;
                row++;

                // Formatear sección PC/PH (verde claro)  
                using (var range = worksheet.Cells[row - 3, 1, row - 1, 2])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }

                // Espacio separador  
                row++;

                // Sección CT/EN  
                worksheet.Cells[row, 1].Value = "CLASIFICACIÓN CT/EN";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                var totalCT = pallets.Count(p => p.EsCT);
                var totalEN = pallets.Count(p => p.EsEN);

                worksheet.Cells[row, 1].Value = "Pallets CT:";
                worksheet.Cells[row, 2].Value = totalCT;
                row++;

                worksheet.Cells[row, 1].Value = "Pallets EN:";
                worksheet.Cells[row, 2].Value = totalEN;
                row++;

                // Formatear sección CT/EN (azul claro)  
                using (var range = worksheet.Cells[row - 3, 1, row - 1, 2])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }
            }

            return row + 1;
        }

        private void AddMetadataSection(ExcelWorksheet worksheet, int startRow, ReportMetadata metadata)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "INFORMACIÓN DEL REPORTE";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Generado el:";
            worksheet.Cells[row, 2].Value = metadata.GeneratedAt.ToString("dd/MM/yyyy HH:mm:ss");
            row++;

            worksheet.Cells[row, 1].Value = "Generado por:";
            worksheet.Cells[row, 2].Value = metadata.GeneratedBy;
            row++;

            worksheet.Cells[row, 1].Value = "Versión:";
            worksheet.Cells[row, 2].Value = metadata.Version;
        }
    }
}