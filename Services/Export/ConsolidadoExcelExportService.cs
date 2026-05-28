// Services/Export/ConsolidadoExcelExportService.cs  
using AplicacionDespacho.Models.Reports;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AplicacionDespacho.Services.Export
{
    public class ConsolidadoExcelExportService
    {
        public async Task<bool> ExportarReporteConsolidado(
            List<ReporteGeneralPallet> pallets,
            DateTime fechaDesde,
            DateTime fechaHasta,
            string filePath)
        {
            try
            {
                // Configurar EPPlus para uso no comercial  
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Reporte Consolidado");

                    await Task.Run(() => BuildConsolidadoWorksheet(worksheet, pallets, fechaDesde, fechaHasta));

                    var fileInfo = new FileInfo(filePath);
                    await package.SaveAsAsync(fileInfo);

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void BuildConsolidadoWorksheet(ExcelWorksheet worksheet, List<ReporteGeneralPallet> pallets, DateTime fechaDesde, DateTime fechaHasta)
        {
            int currentRow = 1;

            // Título del reporte  
            currentRow = AddTitle(worksheet, currentRow, "REPORTE CONSOLIDADO DE PALLETS ENVIADOS");
            currentRow += 2;

            // Información del período  
            currentRow = AddPeriodInfo(worksheet, currentRow, fechaDesde, fechaHasta, pallets.Count);
            currentRow += 2;

            // Detalle de pallets  
            currentRow = AddPalletsDetail(worksheet, currentRow, pallets);
            currentRow += 2;

            // Resumen por variedad  
            currentRow = AddResumenVariedad(worksheet, currentRow, pallets);
            currentRow += 2;

            // Resumen por empresa  
            currentRow = AddResumenEmpresa(worksheet, currentRow, pallets);
            currentRow += 2;

            // Totales generales  
            AddTotalesGenerales(worksheet, currentRow, pallets);

            // Ajustar columnas  
            worksheet.Cells.AutoFitColumns();
        }

        private int AddTitle(ExcelWorksheet worksheet, int startRow, string title)
        {
            worksheet.Cells[startRow, 1].Value = title;
            worksheet.Cells[startRow, 1, startRow, 12].Merge = true;
            worksheet.Cells[startRow, 1].Style.Font.Size = 16;
            worksheet.Cells[startRow, 1].Style.Font.Bold = true;
            worksheet.Cells[startRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            return startRow + 1;
        }

        private int AddPeriodInfo(ExcelWorksheet worksheet, int startRow, DateTime fechaDesde, DateTime fechaHasta, int totalPallets)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "PERÍODO DEL REPORTE";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Desde:";
            worksheet.Cells[row, 2].Value = fechaDesde.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 3].Value = "Hasta:";
            worksheet.Cells[row, 4].Value = fechaHasta.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 5].Value = "Total Pallets:";
            worksheet.Cells[row, 6].Value = totalPallets;

            return row + 1;
        }

        private int AddPalletsDetail(ExcelWorksheet worksheet, int startRow, List<ReporteGeneralPallet> pallets)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "DETALLE DE PALLETS ENVIADOS";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            // Encabezados actualizados  
            string[] headers = {
        "Fecha Envío", "N° Viaje", "N° Guía", "Responsable", "Pallet",
        "Variedad", "Calibre", "Embalaje", "N° Cajas", "Peso Total (kg)",
        "Empresa", "Conductor", "Placa", "Origen", "Destino", "Modificado"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[row, i + 1].Value = headers[i];
            }

            // Formatear encabezados  
            using (var range = worksheet.Cells[row, 1, row, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
            row++;

            // Datos de pallets actualizados  
            foreach (var pallet in pallets)
            {
                worksheet.Cells[row, 1].Value = pallet.FechaEnvio?.ToString("dd/MM/yyyy") ?? "";
                worksheet.Cells[row, 2].Value = pallet.NumeroViaje;
                worksheet.Cells[row, 3].Value = pallet.NumeroGuia;
                worksheet.Cells[row, 4].Value = pallet.Responsable;
                worksheet.Cells[row, 5].Value = pallet.NumeroPallet;
                worksheet.Cells[row, 6].Value = pallet.VariedadParaReporte;
                worksheet.Cells[row, 7].Value = pallet.Calibre;
                worksheet.Cells[row, 8].Value = pallet.Embalaje;
                worksheet.Cells[row, 9].Value = pallet.TotalCajasDisplay; // Para mostrar formato detallado
                worksheet.Cells[row, 10].Value = pallet.PesoTotal;
                worksheet.Cells[row, 11].Value = pallet.NombreEmpresa;
                worksheet.Cells[row, 12].Value = pallet.NombreConductor;
                worksheet.Cells[row, 13].Value = pallet.PlacaVehiculo;
                worksheet.Cells[row, 14].Value = pallet.PuntoPartida;
                worksheet.Cells[row, 15].Value = pallet.PuntoLlegada;
                worksheet.Cells[row, 16].Value = pallet.Modificado ? "Sí" : "No";

                // Formatear números  
                worksheet.Cells[row, 10].Style.Numberformat.Format = "0.000";
                row++;
            }

            return row;
        }

        private int AddResumenVariedad(ExcelWorksheet worksheet, int startRow, List<ReporteGeneralPallet> pallets)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "RESUMEN POR VARIEDAD";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            // Encabezados  
            worksheet.Cells[row, 1].Value = "Variedad";
            worksheet.Cells[row, 2].Value = "Pallets";
            worksheet.Cells[row, 3].Value = "Cajas";
            worksheet.Cells[row, 4].Value = "Kilos";

            using (var range = worksheet.Cells[row, 1, row, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }
            row++;

            var resumenVariedad = pallets
                .GroupBy(p => p.VariedadParaReporte)
                .Select(g => new
                {
                    Variedad = g.Key,
                    TotalPallets = g.Count(),
                    TotalCajas = g.Sum(p => p.CajasParaReporte),
                    TotalKilos = g.Sum(p => p.PesoTotal)
                })
                .OrderBy(r => r.Variedad);

            foreach (var item in resumenVariedad)
            {
                worksheet.Cells[row, 1].Value = item.Variedad;
                worksheet.Cells[row, 2].Value = item.TotalPallets;
                worksheet.Cells[row, 3].Value = item.TotalCajas;
                worksheet.Cells[row, 4].Value = item.TotalKilos;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "0.000";
                row++;
            }

            return row;
        }

        private int AddResumenEmpresa(ExcelWorksheet worksheet, int startRow, List<ReporteGeneralPallet> pallets)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "RESUMEN POR EMPRESA";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            // Encabezados actualizados  
            worksheet.Cells[row, 1].Value = "Empresa";
            worksheet.Cells[row, 2].Value = "Pallets";
            worksheet.Cells[row, 3].Value = "Cajas";
            worksheet.Cells[row, 4].Value = "Kilos";
            worksheet.Cells[row, 5].Value = "Viajes";

            using (var range = worksheet.Cells[row, 1, row, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
            }
            row++;

            var resumenEmpresa = pallets
                .GroupBy(p => p.NombreEmpresa)
                .Select(g => new
                {
                    Empresa = g.Key,
                    TotalPallets = g.Count(),
                    TotalCajas = g.Sum(p => p.CajasParaReporte),
                    TotalKilos = g.Sum(p => p.PesoTotal),
                    CantidadViajes = g.Select(p => p.ViajeId).Distinct().Count()
                })
                .OrderBy(r => r.Empresa);

            foreach (var item in resumenEmpresa)
            {
                worksheet.Cells[row, 1].Value = item.Empresa;
                worksheet.Cells[row, 2].Value = item.TotalPallets;
                worksheet.Cells[row, 3].Value = item.TotalCajas;
                worksheet.Cells[row, 4].Value = item.TotalKilos;
                worksheet.Cells[row, 5].Value = item.CantidadViajes;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "0.000";
                row++;
            }

            return row;
        }

        private void AddTotalesGenerales(ExcelWorksheet worksheet, int startRow, List<ReporteGeneralPallet> pallets)
        {
            int row = startRow;

            worksheet.Cells[row, 1].Value = "TOTALES GENERALES";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Total Pallets:";
            worksheet.Cells[row, 2].Value = pallets.Count;
            row++;

            worksheet.Cells[row, 1].Value = "Total Cajas:";
            worksheet.Cells[row, 2].Value = pallets.Sum(p => p.CajasParaReporte);
            row++;

            worksheet.Cells[row, 1].Value = "Total Kilos:";
            worksheet.Cells[row, 2].Value = pallets.Sum(p => p.PesoTotal);
            worksheet.Cells[row, 2].Style.Numberformat.Format = "0.000";

            // NUEVO: Detección condicional CT/EN (igual que en ImpresionDespachoWindow)  
            bool tieneCTEN = pallets.Any(p => p.EsCT || p.EsEN);

            if (!tieneCTEN)
            {
                // CASO 1: Solo PC/PH - Mantener formato original  
                row += 2;
                worksheet.Cells[row, 1].Value = "CLASIFICACIÓN PC/PH";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                var totalPC = pallets.Count(p => p.EsPC);
                var totalPH = pallets.Count(p => p.EsPH);

                worksheet.Cells[row, 1].Value = "Pallets PC (Completos):";
                worksheet.Cells[row, 2].Value = totalPC;
                row++;

                worksheet.Cells[row, 1].Value = "Pallets PH (Puchos):";
                worksheet.Cells[row, 2].Value = totalPH;

                // Formatear sección PC/PH  
                using (var range = worksheet.Cells[row - 1, 1, row, 2])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCyan);
                }
            }
            else
            {
                // CASO 2: Hay CT/EN - Implementar clasificación completa  
                row += 2;
                worksheet.Cells[row, 1].Value = "CLASIFICACIÓN COMPLETA";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                var totalPC = pallets.Count(p => p.EsPC);
                var totalPH = pallets.Count(p => p.EsPH);
                var totalCT = pallets.Count(p => p.EsCT);
                var totalEN = pallets.Count(p => p.EsEN);

                // Sección PC/PH  
                worksheet.Cells[row, 1].Value = "Pallets PC (Completos):";
                worksheet.Cells[row, 2].Value = totalPC;
                row++;

                worksheet.Cells[row, 1].Value = "Pallets PH (Puchos):";
                worksheet.Cells[row, 2].Value = totalPH;
                row++;

                // Sección CT/EN  
                worksheet.Cells[row, 1].Value = "Pallets CT (Contra Muestra):";
                worksheet.Cells[row, 2].Value = totalCT;
                row++;

                worksheet.Cells[row, 1].Value = "Pallets EN (Ensayo):";
                worksheet.Cells[row, 2].Value = totalEN;

                // Formatear sección PC/PH (verde)  
                using (var range = worksheet.Cells[row - 3, 1, row - 2, 2])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                }

                // Formatear sección CT/EN (azul)  
                using (var range = worksheet.Cells[row - 1, 1, row, 2])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }
            }

            // Formatear totales generales (amarillo)  
            using (var range = worksheet.Cells[startRow, 1, startRow + 3, 2])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
            }
        }
    }
}