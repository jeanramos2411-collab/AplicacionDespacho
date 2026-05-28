// ImpresionDespachoWindow.xaml.cs    
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AplicacionDespacho.Models;
using AplicacionDespacho.Models.Reports;

namespace AplicacionDespacho
{
    public partial class ImpresionDespachoWindow : Window
    {
        private Viaje _viaje;
        private List<ResumenPorVariedad> _resumenPorVariedad;
        private List<InformacionPallet> _pallets;
        private List<ResumenVariedadEmbalaje> _resumen;

        public ImpresionDespachoWindow(Viaje viaje, List<InformacionPallet> pallets)
        {
            InitializeComponent();
            _viaje = viaje;
            _pallets = pallets;
            CargarDatos();
        }

        private void CargarDatos()
        {
            // Cargar información del viaje    
            txtFecha.Text = _viaje.Fecha.ToString("dd/MM/yyyy");
            txtNumeroViaje.Text = _viaje.NumeroViaje.ToString();
            txtNumeroGuia.Text = _viaje.NumeroGuia;
            txtResponsable.Text = _viaje.Responsable;
            txtEmpresa.Text = _viaje.NombreEmpresa;
            txtConductor.Text = _viaje.NombreConductor;
            txtPlaca.Text = _viaje.PlacaVehiculo;
            txtDestino.Text = _viaje.PuntoLlegada;

            // Cargar pallets    
            dgPallets.ItemsSource = _pallets;

            // Generar resumen por variedad y embalaje    
            GenerarResumen();

            // Calcular totales generales    
            var totalCajas = _pallets.Sum(p => p.NumeroDeCajas);
            var totalKilos = _pallets.Sum(p => p.PesoTotal);
            var totalPallets = _pallets.Count;
            txtTotalCajasGeneral.Text = $"{totalPallets} pallets - {totalCajas} cajas";
            txtTotalKilosGeneral.Text = totalKilos.ToString("F3");
        }
        private void GenerarResumen()
        {
            // Resumen existente por variedad-embalaje (mantener para compatibilidad)      
            _resumen = _pallets
                .GroupBy(p => new { p.VariedadParaReporte, p.Embalaje })
                .Select(g => new ResumenVariedadEmbalaje
                {
                    VariedadEmbalaje = $"{g.Key.VariedadParaReporte} - {g.Key.Embalaje}",
                    TotalCajas = g.Sum(p => p.CajasParaReporte), // ✅ CORREGIDO  
                    TotalKilos = g.Sum(p => p.PesoTotal)
                })
                .OrderBy(r => r.VariedadEmbalaje)
                .ToList();

            //  NUEVO: Resumen por variedad con detalles de embalaje      
            _resumenPorVariedad = _pallets
                .GroupBy(p => p.VariedadParaReporte)
                .Select(g => new ResumenPorVariedad
                {
                    Variedad = g.Key,
                    TotalCajas = g.Sum(p => p.CajasParaReporte), // ✅ CORREGIDO  
                    TotalKilos = g.Sum(p => p.PesoTotal),
                    TotalPallets = g.Count(),
                    DetallesPorEmbalaje = g.GroupBy(p => p.Embalaje)
                        .Select(embalajeGroup => new ResumenVariedadEmbalaje
                        {
                            VariedadEmbalaje = $"{g.Key} - {embalajeGroup.Key}",
                            TotalCajas = embalajeGroup.Sum(p => p.CajasParaReporte), // ✅ CORREGIDO  
                            TotalKilos = embalajeGroup.Sum(p => p.PesoTotal)
                        })
                        .OrderBy(e => e.VariedadEmbalaje)
                        .ToList()
                })
                .OrderBy(r => r.Variedad)
                .ToList();

            dgResumen.ItemsSource = _resumen;
            // NUEVO: Contadores simples PC/PH para la ventana    
            var totalPC = _pallets.Count(p => p.EsPC);
            var totalPH = _pallets.Count(p => p.EsPH);
            var totalCT = _pallets.Count(p => p.EsCT);
            var totalEN = _pallets.Count(p => p.EsEN);

            // Mostrar en controles de texto  
            txtTotalPC.Text = $"PC: {totalPC}";
            txtTotalPH.Text = $"PH: {totalPH}";
            txtTotalCT.Text = $"CT: {totalCT}";
            txtTotalEN.Text = $"EN: {totalEN}";
        }
        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Crear documento para impresión    
                    FlowDocument documento = CrearDocumentoImpresion();

                    // Configurar documento para impresión    
                    documento.PageHeight = printDialog.PrintableAreaHeight;
                    documento.PageWidth = printDialog.PrintableAreaWidth;
                    documento.PagePadding = new Thickness(30);
                    documento.ColumnGap = 0;
                    documento.ColumnWidth = printDialog.PrintableAreaWidth;

                    // Imprimir    
                    IDocumentPaginatorSource idpSource = documento;
                    printDialog.PrintDocument(idpSource.DocumentPaginator, "Despacho - " + _viaje.NumeroGuia);

                    MessageBox.Show("Documento enviado a impresión correctamente.", "Impresión",
                                   MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnVistaPrevia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Crear ventana de vista previa      
                var ventanaPrevia = new Window
                {
                    Title = "Vista Previa - Despacho " + _viaje.NumeroGuia,
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                // ✅ ALTERNATIVA: Usar FlowDocumentPageViewer    
                var documentViewer = new FlowDocumentPageViewer();
                FlowDocument documento = CrearDocumentoImpresion();
                documentViewer.Document = documento;

                ventanaPrevia.Content = documentViewer;
                ventanaPrevia.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al mostrar vista previa: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CrearDocumentoImpresion()
        {
            FlowDocument documento = new FlowDocument();

            // ✅ CONFIGURACIÓN OPTIMIZADA PARA UNA PÁGINA    
            documento.PageHeight = 11 * 96; // 11 pulgadas    
            documento.PageWidth = 8.5 * 96; // 8.5 pulgadas      
            documento.PagePadding = new Thickness(30); // Márgenes más pequeños    
            documento.ColumnGap = 0;
            documento.FontFamily = new FontFamily("Arial");
            documento.FontSize = 10; // Fuente más pequeña para más contenido    

            // Título más compacto    
            Paragraph titulo = new Paragraph(new Run("DESPACHO SANTA MARIA"))
            {
                FontSize = 16, // Reducido de 18    
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10) // Reducido de 20    
            };
            documento.Blocks.Add(titulo);

            // Información del viaje en formato más compacto    
            Table tablaInfo = CrearTablaInfoCompacta();
            documento.Blocks.Add(tablaInfo);

            // Espacio reducido    
            documento.Blocks.Add(new Paragraph(new Run(" ")) { Margin = new Thickness(0, 3, 0, 3) });

            // Tabla de pallets optimizada    
            Paragraph tituloPallets = new Paragraph(new Run("DETALLE DE PALLETS"))
            {
                FontSize = 12, // Reducido    
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5) // Reducido    
            };
            documento.Blocks.Add(tituloPallets);

            Table tablaPallets = CrearTablaPalletsOptimizada();
            documento.Blocks.Add(tablaPallets);

            // Espacio mínimo    
            documento.Blocks.Add(new Paragraph(new Run(" ")) { Margin = new Thickness(0, 5, 0, 5) });

            // Tabla resumen y totales en la misma línea    
            Table tablaResumenYTotales = CrearTablaResumenYTotales();
            documento.Blocks.Add(tablaResumenYTotales);

            return documento;
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private Table CrearTablaInfoCompacta()
        {
            Table tabla = new Table();
            tabla.CellSpacing = 0;
            tabla.BorderBrush = Brushes.Black;
            tabla.BorderThickness = new Thickness(1);
            tabla.Margin = new Thickness(0, 0, 0, 10);

            // 4 columnas para información más compacta    
            tabla.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup grupo = new TableRowGroup();

            // Fila 1: Fecha, N° Viaje, N° Guía, Responsable    
            TableRow fila1 = new TableRow();
            fila1.Cells.Add(CrearCeldaInfo("Fecha:", _viaje.Fecha.ToString("dd/MM/yyyy")));
            fila1.Cells.Add(CrearCeldaInfo("N° Viaje:", _viaje.NumeroViaje.ToString()));
            fila1.Cells.Add(CrearCeldaInfo("N° Guía:", _viaje.NumeroGuia));
            fila1.Cells.Add(CrearCeldaInfo("Responsable:", _viaje.Responsable));
            grupo.Rows.Add(fila1);

            // Fila 2: Empresa, Conductor, Placa, Punto Partida    
            TableRow fila2 = new TableRow();
            fila2.Cells.Add(CrearCeldaInfo("Empresa:", _viaje.NombreEmpresa));
            fila2.Cells.Add(CrearCeldaInfo("Conductor:", _viaje.NombreConductor));
            fila2.Cells.Add(CrearCeldaInfo("Placa:", _viaje.PlacaVehiculo ?? "N/A"));
            fila2.Cells.Add(CrearCeldaInfo("Punto Partida:", _viaje.PuntoPartida ?? "N/A"));
            grupo.Rows.Add(fila2);

            // Fila 3: Solo Destino (centrado)    
            TableRow fila3 = new TableRow();
            TableCell celdaDestino = CrearCeldaInfo("Destino:", _viaje.PuntoLlegada ?? "N/A");
            celdaDestino.ColumnSpan = 4; // Ocupa las 4 columnas    
            fila3.Cells.Add(celdaDestino);
            grupo.Rows.Add(fila3);

            tabla.RowGroups.Add(grupo);
            return tabla;
        }

        private TableCell CrearCeldaInfo(string etiqueta, string valor)
        {
            TableCell celda = new TableCell();
            celda.BorderBrush = Brushes.Black;
            celda.BorderThickness = new Thickness(0.5);
            celda.Padding = new Thickness(4);

            Paragraph contenido = new Paragraph();
            contenido.Inlines.Add(new Run(etiqueta) { FontWeight = FontWeights.Bold, FontSize = 10 });
            contenido.Inlines.Add(new LineBreak());
            contenido.Inlines.Add(new Run(valor) { FontSize = 10 });
            contenido.Margin = new Thickness(0);

            celda.Blocks.Add(contenido);
            return celda;
        }
        private Table CrearTablaPalletsOptimizada()
        {
            Table tabla = new Table();
            tabla.CellSpacing = 0;
            tabla.BorderBrush = Brushes.Black;
            tabla.BorderThickness = new Thickness(1);
            tabla.FontSize = 12; // Reducir ligeramente para más espacio  

            // ✅ COLUMNAS OPTIMIZADAS CON ANCHOS MEJORADOS  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Pallet - más ancho  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(175) }); // Variedad - más ancho  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(55) }); // Calibre  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Embalaje - más ancho  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(95) }); // Cajas  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(65) }); // Peso Unit.  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(75) }); // Peso Total  

            // Encabezados optimizados  
            TableRowGroup encabezados = new TableRowGroup();
            TableRow filaEncabezado = new TableRow();
            filaEncabezado.Background = Brushes.LightGray;

            string[] headers = { "Pallet", "Variedad", "Calibre", "Embalaje", "Cajas", "P.Unit", "P.Total" };
            foreach (string header in headers)
            {
                TableCell celda = new TableCell();

                // ✅ PÁRRAFO OPTIMIZADO PARA EVITAR SALTOS DE LÍNEA  
                Paragraph parrafo = new Paragraph(new Run(header)
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 12
                });
                parrafo.Margin = new Thickness(0); // Eliminar márgenes del párrafo  
                parrafo.TextAlignment = TextAlignment.Center;

                celda.Blocks.Add(parrafo);
                celda.BorderBrush = Brushes.Black;
                celda.BorderThickness = new Thickness(0.5);
                celda.Padding = new Thickness(4, 2, 4, 2); // Padding horizontal mayor, vertical menor  
                celda.TextAlignment = TextAlignment.Center;

                filaEncabezado.Cells.Add(celda);
            }
            encabezados.Rows.Add(filaEncabezado);
            tabla.RowGroups.Add(encabezados);

            // ✅ DATOS OPTIMIZADOS PARA EVITAR SALTOS DE LÍNEA  
            TableRowGroup datos = new TableRowGroup();
            foreach (var pallet in _pallets)
            {
                TableRow fila = new TableRow();

                string[] valores = {
            pallet.NumeroPallet,
            TruncateText(pallet.VariedadParaReporte, 30), // Truncar si es muy largo  
            pallet.Calibre,
            TruncateText(pallet.Embalaje, 30), // Truncar si es muy largo  
            pallet.EsBicolor ?
            $"{pallet.CajasParaReporte}" :
            //CODIGO MUESTRA TOTAL DE CAJAS BICOLOR + EL NUMERO DE CAJAS POR CADA VARIEDAD
            //$"{pallet.CajasParaReporte} ({pallet.NumeroDeCajas}+{pallet.CajasSegundaVariedad})" :
            pallet.CajasParaReporte.ToString(),
            pallet.PesoUnitario.ToString("F1"),
            pallet.PesoTotal.ToString("F1")
        };

                for (int i = 0; i < valores.Length; i++)
                {
                    TableCell celda = new TableCell();

                    // ✅ PÁRRAFO OPTIMIZADO SIN MÁRGENES  
                    Paragraph parrafo = new Paragraph(new Run(valores[i])
                    {
                        FontSize = 12
                    });
                    parrafo.Margin = new Thickness(0); // Eliminar márgenes  

                    // Alineación específica por columna  
                    if (i == 4 || i == 5 || i == 6) // Cajas, Peso Unit, Peso Total  
                    {
                        parrafo.TextAlignment = TextAlignment.Right;
                    }
                    else
                    {
                        parrafo.TextAlignment = TextAlignment.Left;
                    }

                    celda.Blocks.Add(parrafo);
                    celda.BorderBrush = Brushes.Black;
                    celda.BorderThickness = new Thickness(0.5);
                    celda.Padding = new Thickness(4, 2, 4, 2); // Padding optimizado  

                    fila.Cells.Add(celda);
                }
                datos.Rows.Add(fila);
            }
            tabla.RowGroups.Add(datos);

            return tabla;
        }

        // ✅ MÉTODO AUXILIAR PARA TRUNCAR TEXTO LARGO  
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 3) + "...";
        }
        private Table CrearTablaResumenYTotales()
        {
            Table tabla = new Table();
            tabla.CellSpacing = 0;
            tabla.BorderBrush = Brushes.Black;
            tabla.BorderThickness = new Thickness(1);

            // PASO 1: Detectar presencia de pallets CT/EN  
            bool tieneCTEN = _pallets.Any(p => p.EsCT || p.EsEN);

            if (!tieneCTEN)
            {
                // CASO 1: NO hay CT/EN - Mantener estructura original de 2 columnas  
                return CrearTablaOriginal();
            }
            else
            {
                // CASO 2: HAY CT/EN - Implementar filas independientes  
                return CrearTablaConFilasIndependientes();
            }
        }

        private Table CrearTablaOriginal()
        {
            Table tabla = new Table();
            tabla.CellSpacing = 0;
            tabla.BorderBrush = Brushes.Black;
            tabla.BorderThickness = new Thickness(1);

            // 2 columnas: Resumen a la izquierda, Totales a la derecha  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            TableRowGroup grupo = new TableRowGroup();
            TableRow fila = new TableRow();

            // Celda izquierda: Resumen por variedad con embalajes        
            TableCell celdaResumen = new TableCell();
            celdaResumen.BorderBrush = Brushes.Black;
            celdaResumen.BorderThickness = new Thickness(0.5);
            celdaResumen.Padding = new Thickness(6);

            Paragraph tituloResumen = new Paragraph(new Run("RESUMEN POR VARIEDAD Y EMBALAJE"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            };
            celdaResumen.Blocks.Add(tituloResumen);

            // ✅ MOSTRAR SUBTOTALES POR VARIEDAD CON DETALLES DE EMBALAJE Y CONTADORES PC/PH    
            foreach (var variedad in _resumenPorVariedad)
            {
                // NUEVO: Calcular contadores PC/PH para esta variedad específica  
                var palletsDeEstaVariedad = _pallets.Where(p => p.VariedadParaReporte == variedad.Variedad).ToList();
                var totalPCVariedad = palletsDeEstaVariedad.Count(p => p.EsPC);
                var totalPHVariedad = palletsDeEstaVariedad.Count(p => p.EsPH);
                var totalCTVariedad = palletsDeEstaVariedad.Count(p => p.EsCT);
                var totalENVariedad = palletsDeEstaVariedad.Count(p => p.EsEN);

                // Subtotal por variedad (encabezado principal) - USAR VariedadParaReporte para mostrar variedades bicolor completas    
                Paragraph lineaVariedad = new Paragraph()
                {
                    Margin = new Thickness(0, 3, 0, 2)
                };
                lineaVariedad.Inlines.Add(new Run($"• {variedad.Variedad}: ")
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    TextDecorations = TextDecorations.Underline
                });

                // MEJORADO: Formato con contadores PC/PH/CT por variedad  
                string contadoresPCPHCTEN = "";
                var contadores = new List<string>();

                if (totalPCVariedad > 0) contadores.Add($"{totalPCVariedad} PC");
                if (totalPHVariedad > 0) contadores.Add($"{totalPHVariedad} PH");
                if (totalCTVariedad > 0) contadores.Add($"{totalCTVariedad} CT");
                if (totalENVariedad > 0) contadores.Add($"{totalENVariedad} EN");

                if (contadores.Count > 0)
                {
                    if (contadores.Count == 1)
                        contadoresPCPHCTEN = $", {contadores[0]}";
                    else if (contadores.Count == 2)
                        contadoresPCPHCTEN = $", {contadores[0]} y {contadores[1]}";
                    else
                        contadoresPCPHCTEN = $", {string.Join(", ", contadores.Take(contadores.Count - 1))} y {contadores.Last()}";
                }

                lineaVariedad.Inlines.Add(new Run($"{variedad.TotalPallets} pallets{contadoresPCPHCTEN}, {variedad.TotalCajas} cajas, {variedad.TotalKilos:F1} kg")
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                });
                celdaResumen.Blocks.Add(lineaVariedad);

                // Detalles por embalaje dentro de cada variedad        
                foreach (var detalle in variedad.DetallesPorEmbalaje)
                {
                    Paragraph lineaDetalle = new Paragraph()
                    {
                        Margin = new Thickness(20, 1, 0, 1) // Indentado más profundo        
                    };

                    // Extraer solo el nombre del embalaje (después del guión)        
                    string nombreEmbalaje = detalle.VariedadEmbalaje.Split('-')[1].Trim();

                    lineaDetalle.Inlines.Add(new Run($"◦ {nombreEmbalaje}: ")
                    {
                        FontSize = 10,
                        FontStyle = FontStyles.Italic
                    });
                    lineaDetalle.Inlines.Add(new Run($"{detalle.TotalCajas} cajas, {detalle.TotalKilos:F1} kg")
                    {
                        FontSize = 10
                    });
                    celdaResumen.Blocks.Add(lineaDetalle);
                }
            }

            // Celda derecha: Totales generales CON CONTADORES PC/PH    
            TableCell celdaTotales = new TableCell();
            celdaTotales.BorderBrush = Brushes.Black;
            celdaTotales.BorderThickness = new Thickness(0.5);
            celdaTotales.Padding = new Thickness(6);
            celdaTotales.Background = Brushes.LightYellow;

            // USAR PROPIEDADES BICOLOR PARA TOTALES CORRECTOS    
            var totalCajas = _pallets.Sum(p => p.CajasParaReporte);
            var totalKilos = _pallets.Sum(p => p.PesoTotalBicolor);
            var totalPallets = _pallets.Count;

            // NUEVOS CONTADORES PC/PH    
            // NUEVOS CONTADORES PC/PH/CT      
            var totalPC = _pallets.Count(p => p.EsPC);
            var totalPH = _pallets.Count(p => p.EsPH);
            var totalCT = _pallets.Count(p => p.EsCT);
            var totalEN = _pallets.Count(p => p.EsEN);

            Paragraph totales = new Paragraph()
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            totales.Inlines.Add(new Run("TOTAL GENERAL") { FontSize = 12 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Pallets: {totalPallets}") { FontSize = 14 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Cajas: {totalCajas}") { FontSize = 16 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Kilos: {totalKilos:F1}") { FontSize = 16 });

            // AGREGAR SEPARADOR Y CONTADORES PC/PH    
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run("CLASIFICACIÓN") { FontSize = 11, FontWeight = FontWeights.Bold });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"PC: {totalPC} pallets")
            {
                FontSize = 12,
                Foreground = Brushes.DarkGreen
            });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"PH: {totalPH} pallets")
            {
                FontSize = 12,
                Foreground = Brushes.DarkOrange
            });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"CT: {totalCT} pallets")
            {
                FontSize = 12,
                Foreground = Brushes.DarkBlue
            });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"EN: {totalEN} pallets")
            {
                FontSize = 12,
                Foreground = Brushes.DarkMagenta
            });

            celdaTotales.Blocks.Add(totales);

            fila.Cells.Add(celdaResumen);
            fila.Cells.Add(celdaTotales);
            grupo.Rows.Add(fila);
            tabla.RowGroups.Add(grupo);

            return tabla;
        }
        private Table CrearTablaConFilasIndependientes()
        {
            Table tabla = new Table();
            tabla.CellSpacing = 0;
            tabla.BorderBrush = Brushes.Black;
            tabla.BorderThickness = new Thickness(1);

            // 3 columnas para la distribución de 6 secciones  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) }); // Resúmenes  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Totales  
            tabla.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Clasificación/Total General  

            TableRowGroup grupo = new TableRowGroup();

            // FILA 1: PC/PH  
            TableRow filaPCPH = new TableRow();
            filaPCPH.Cells.Add(CrearResumenPCPH());
            filaPCPH.Cells.Add(CrearTotalPCPH());

            // Celda de clasificación que se extiende a ambas filas  
            TableCell celdaClasificacion = CrearClasificacionYTotalGeneral();
            celdaClasificacion.RowSpan = 2; // Se extiende a ambas filas  
            filaPCPH.Cells.Add(celdaClasificacion);

            grupo.Rows.Add(filaPCPH);

            // FILA 2: CT/EN  
            TableRow filaCTEN = new TableRow();
            filaCTEN.Cells.Add(CrearResumenCTEN());
            filaCTEN.Cells.Add(CrearTotalCTEN());
            // No agregar tercera celda porque la clasificación ya se extiende desde la fila anterior  

            grupo.Rows.Add(filaCTEN);

            tabla.RowGroups.Add(grupo);
            return tabla;
        }
        private TableCell CrearClasificacionYTotalGeneral()
        {
            TableCell celda = new TableCell();
            celda.BorderBrush = Brushes.Black;
            celda.BorderThickness = new Thickness(0.5);
            celda.Padding = new Thickness(6);
            celda.Background = Brushes.LightYellow;

            // Total General (parte superior)  
            var totalCajas = _pallets.Sum(p => p.CajasParaReporte);
            var totalKilos = _pallets.Sum(p => p.PesoTotalBicolor);
            var totalPallets = _pallets.Count;

            Paragraph totalGeneral = new Paragraph()
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            totalGeneral.Inlines.Add(new Run("TOTAL GENERAL") { FontSize = 12 });
            totalGeneral.Inlines.Add(new LineBreak());
            totalGeneral.Inlines.Add(new Run($"Pallets: {totalPallets}") { FontSize = 14 });
            totalGeneral.Inlines.Add(new LineBreak());
            totalGeneral.Inlines.Add(new Run($"Cajas: {totalCajas}") { FontSize = 16 });
            totalGeneral.Inlines.Add(new LineBreak());
            totalGeneral.Inlines.Add(new Run($"Kilos: {totalKilos:F1}") { FontSize = 16 });

            celda.Blocks.Add(totalGeneral);

            // Clasificación (parte inferior)  
            var totalPC = _pallets.Count(p => p.EsPC);
            var totalPH = _pallets.Count(p => p.EsPH);
            var totalCT = _pallets.Count(p => p.EsCT);
            var totalEN = _pallets.Count(p => p.EsEN);

            Paragraph clasificacion = new Paragraph()
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };

            clasificacion.Inlines.Add(new Run("CLASIFICACIÓN") { FontSize = 11 });
            clasificacion.Inlines.Add(new LineBreak());
            clasificacion.Inlines.Add(new Run($"PC: {totalPC}") { FontSize = 12, Foreground = Brushes.DarkGreen });
            clasificacion.Inlines.Add(new LineBreak());
            clasificacion.Inlines.Add(new Run($"PH: {totalPH}") { FontSize = 12, Foreground = Brushes.DarkOrange });
            clasificacion.Inlines.Add(new LineBreak());
            clasificacion.Inlines.Add(new Run($"CT: {totalCT}") { FontSize = 12, Foreground = Brushes.DarkBlue });
            clasificacion.Inlines.Add(new LineBreak());
            clasificacion.Inlines.Add(new Run($"EN: {totalEN}") { FontSize = 12, Foreground = Brushes.DarkMagenta });

            celda.Blocks.Add(clasificacion);
            return celda;
        }

        private TableCell CrearResumenPCPH()
        {
            TableCell celda = new TableCell();
            celda.BorderBrush = Brushes.Black;
            celda.BorderThickness = new Thickness(0.5);
            celda.Padding = new Thickness(6);

            Paragraph titulo = new Paragraph(new Run("RESUMEN PC/PH POR VARIEDAD Y EMBALAJE"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            };
            celda.Blocks.Add(titulo);

            // Filtrar solo pallets PC y PH  
            var palletsPCPH = _pallets.Where(p => p.EsPC || p.EsPH).ToList();

            // Crear resumen por variedad solo para PC/PH  
            var resumenPCPH = palletsPCPH
                .GroupBy(p => p.VariedadParaReporte)
                .Select(g => new ResumenPorVariedad
                {
                    Variedad = g.Key,
                    TotalCajas = g.Sum(p => p.CajasParaReporte),
                    TotalKilos = g.Sum(p => p.PesoTotal),
                    TotalPallets = g.Count(),
                    DetallesPorEmbalaje = g.GroupBy(p => p.Embalaje)
                        .Select(embalajeGroup => new ResumenVariedadEmbalaje
                        {
                            VariedadEmbalaje = $"{g.Key} - {embalajeGroup.Key}",
                            TotalCajas = embalajeGroup.Sum(p => p.CajasParaReporte),
                            TotalKilos = embalajeGroup.Sum(p => p.PesoTotal)
                        })
                        .OrderBy(e => e.VariedadEmbalaje)
                        .ToList()
                })
                .OrderBy(r => r.Variedad)
                .ToList();

            // Generar el contenido usando la misma lógica que tienes en el código original  
            foreach (var variedad in resumenPCPH)
            {
                var palletsDeEstaVariedad = palletsPCPH.Where(p => p.VariedadParaReporte == variedad.Variedad).ToList();
                var totalPCVariedad = palletsDeEstaVariedad.Count(p => p.EsPC);
                var totalPHVariedad = palletsDeEstaVariedad.Count(p => p.EsPH);

                Paragraph lineaVariedad = new Paragraph()
                {
                    Margin = new Thickness(0, 3, 0, 2)
                };
                lineaVariedad.Inlines.Add(new Run($"• {variedad.Variedad}: ")
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    TextDecorations = TextDecorations.Underline
                });

                string contadoresPCPH = "";
                if (totalPCVariedad > 0 && totalPHVariedad > 0)
                    contadoresPCPH = $", {totalPCVariedad} PC y {totalPHVariedad} PH";
                else if (totalPCVariedad > 0)
                    contadoresPCPH = $", {totalPCVariedad} PC";
                else if (totalPHVariedad > 0)
                    contadoresPCPH = $", {totalPHVariedad} PH";

                lineaVariedad.Inlines.Add(new Run($"{variedad.TotalPallets} pallets{contadoresPCPH}, {variedad.TotalCajas} cajas, {variedad.TotalKilos:F1} kg")
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                });
                celda.Blocks.Add(lineaVariedad);

                // Detalles por embalaje  
                foreach (var detalle in variedad.DetallesPorEmbalaje)
                {
                    Paragraph lineaDetalle = new Paragraph()
                    {
                        Margin = new Thickness(20, 1, 0, 1)
                    };

                    string nombreEmbalaje = detalle.VariedadEmbalaje.Split('-')[1].Trim();
                    lineaDetalle.Inlines.Add(new Run($"◦ {nombreEmbalaje}: ")
                    {
                        FontSize = 10,
                        FontStyle = FontStyles.Italic
                    });
                    lineaDetalle.Inlines.Add(new Run($"{detalle.TotalCajas} cajas, {detalle.TotalKilos:F1} kg")
                    {
                        FontSize = 10
                    });
                    celda.Blocks.Add(lineaDetalle);
                }
            }

            return celda;
        }
        private TableCell CrearTotalPCPH()
        {
            TableCell celda = new TableCell();
            celda.BorderBrush = Brushes.Black;
            celda.BorderThickness = new Thickness(0.5);
            celda.Padding = new Thickness(6);
            celda.Background = Brushes.LightGreen;

            // Filtrar solo pallets PC y PH  
            var palletsPCPH = _pallets.Where(p => p.EsPC || p.EsPH).ToList();

            var totalCajas = palletsPCPH.Sum(p => p.CajasParaReporte);
            var totalKilos = palletsPCPH.Sum(p => p.PesoTotal);
            var totalPallets = palletsPCPH.Count;
            var totalPC = palletsPCPH.Count(p => p.EsPC);
            var totalPH = palletsPCPH.Count(p => p.EsPH);

            Paragraph totales = new Paragraph()
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };

            totales.Inlines.Add(new Run("TOTAL PC/PH") { FontSize = 12 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Pallets: {totalPallets}") { FontSize = 14 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Cajas: {totalCajas}") { FontSize = 16 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Kilos: {totalKilos:F1}") { FontSize = 16 });

            // Separador y clasificación  
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run("CLASIFICACIÓN") { FontSize = 10, FontWeight = FontWeights.Bold });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"PC: {totalPC}") { FontSize = 11, Foreground = Brushes.DarkGreen });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"PH: {totalPH}") { FontSize = 11, Foreground = Brushes.DarkOrange });

            celda.Blocks.Add(totales);
            return celda;
        }
        private TableCell CrearResumenCTEN()
        {
            TableCell celda = new TableCell();
            celda.BorderBrush = Brushes.Black;
            celda.BorderThickness = new Thickness(0.5);
            celda.Padding = new Thickness(6);

            Paragraph titulo = new Paragraph(new Run("RESUMEN CT/EN POR VARIEDAD Y EMBALAJE"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            };
            celda.Blocks.Add(titulo);

            // Filtrar solo pallets CT y EN  
            var palletsCTEN = _pallets.Where(p => p.EsCT || p.EsEN).ToList();

            // Crear resumen por variedad solo para CT/EN  
            var resumenCTEN = palletsCTEN
                .GroupBy(p => p.VariedadParaReporte)
                .Select(g => new ResumenPorVariedad
                {
                    Variedad = g.Key,
                    TotalCajas = g.Sum(p => p.CajasParaReporte),
                    TotalKilos = g.Sum(p => p.PesoTotal),
                    TotalPallets = g.Count(),
                    DetallesPorEmbalaje = g.GroupBy(p => p.Embalaje)
                        .Select(embalajeGroup => new ResumenVariedadEmbalaje
                        {
                            VariedadEmbalaje = $"{g.Key} - {embalajeGroup.Key}",
                            TotalCajas = embalajeGroup.Sum(p => p.CajasParaReporte),
                            TotalKilos = embalajeGroup.Sum(p => p.PesoTotal)
                        })
                        .OrderBy(e => e.VariedadEmbalaje)
                        .ToList()
                })
                .OrderBy(r => r.Variedad)
                .ToList();

            // Generar el contenido usando la misma lógica  
            foreach (var variedad in resumenCTEN)
            {
                var palletsDeEstaVariedad = palletsCTEN.Where(p => p.VariedadParaReporte == variedad.Variedad).ToList();
                var totalCTVariedad = palletsDeEstaVariedad.Count(p => p.EsCT);
                var totalENVariedad = palletsDeEstaVariedad.Count(p => p.EsEN);

                Paragraph lineaVariedad = new Paragraph()
                {
                    Margin = new Thickness(0, 3, 0, 2)
                };
                lineaVariedad.Inlines.Add(new Run($"• {variedad.Variedad}: ")
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    TextDecorations = TextDecorations.Underline
                });

                string contadoresCTEN = "";
                if (totalCTVariedad > 0 && totalENVariedad > 0)
                    contadoresCTEN = $", {totalCTVariedad} CT y {totalENVariedad} EN";
                else if (totalCTVariedad > 0)
                    contadoresCTEN = $", {totalCTVariedad} CT";
                else if (totalENVariedad > 0)
                    contadoresCTEN = $", {totalENVariedad} EN";

                lineaVariedad.Inlines.Add(new Run($"{variedad.TotalPallets} pallets{contadoresCTEN}, {variedad.TotalCajas} cajas, {variedad.TotalKilos:F1} kg")
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Bold
                });
                celda.Blocks.Add(lineaVariedad);

                // Detalles por embalaje  
                foreach (var detalle in variedad.DetallesPorEmbalaje)
                {
                    Paragraph lineaDetalle = new Paragraph()
                    {
                        Margin = new Thickness(20, 1, 0, 1)
                    };

                    string nombreEmbalaje = detalle.VariedadEmbalaje.Split('-')[1].Trim();
                    lineaDetalle.Inlines.Add(new Run($"◦ {nombreEmbalaje}: ")
                    {
                        FontSize = 10,
                        FontStyle = FontStyles.Italic
                    });
                    lineaDetalle.Inlines.Add(new Run($"{detalle.TotalCajas} cajas, {detalle.TotalKilos:F1} kg")
                    {
                        FontSize = 10
                    });
                    celda.Blocks.Add(lineaDetalle);
                }
            }

            return celda;
        }
        private TableCell CrearTotalCTEN()
        {
            TableCell celda = new TableCell();
            celda.BorderBrush = Brushes.Black;
            celda.BorderThickness = new Thickness(0.5);
            celda.Padding = new Thickness(6);
            celda.Background = Brushes.LightBlue; // Color distintivo para CT/EN  

            // Filtrar solo pallets CT/EN  
            var palletsCTEN = _pallets.Where(p => p.EsCT || p.EsEN).ToList();

            var totalPallets = palletsCTEN.Count;
            var totalCajas = palletsCTEN.Sum(p => p.CajasParaReporte);
            var totalKilos = palletsCTEN.Sum(p => p.PesoTotal);
            var totalCT = palletsCTEN.Count(p => p.EsCT);
            var totalEN = palletsCTEN.Count(p => p.EsEN);

            Paragraph totales = new Paragraph()
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };

            totales.Inlines.Add(new Run("TOTAL CT/EN") { FontSize = 12 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Pallets: {totalPallets}") { FontSize = 14 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Cajas: {totalCajas}") { FontSize = 16 });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"Kilos: {totalKilos:F1}") { FontSize = 16 });

            // Separador y clasificación  
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run("CLASIFICACIÓN") { FontSize = 10, FontWeight = FontWeights.Bold });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"CT: {totalCT}") { FontSize = 11, Foreground = Brushes.DarkBlue });
            totales.Inlines.Add(new LineBreak());
            totales.Inlines.Add(new Run($"EN: {totalEN}") { FontSize = 11, Foreground = Brushes.DarkMagenta });

            celda.Blocks.Add(totales);
            return celda;
        }
    }

}