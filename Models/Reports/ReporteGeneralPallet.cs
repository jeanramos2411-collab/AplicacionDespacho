// Models/Reports/ReporteGeneralPallet.cs    
using System;

namespace AplicacionDespacho.Models.Reports
{
    public class ReporteGeneralPallet
    {
        // Información del Pallet    
        public int PalletId { get; set; }
        public string NumeroPallet { get; set; }
        public string Variedad { get; set; }
        public string Calibre { get; set; }
        public string Embalaje { get; set; }
        public int NumeroDeCajas { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal PesoTotal { get; set; }
        public DateTime FechaEscaneo { get; set; }
        public bool Modificado { get; set; }

        // Información de Envío    
        public string EstadoEnvio { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public string UsuarioEnvio { get; set; }

        // Información Completa del Viaje    
        public int ViajeId { get; set; }
        public DateTime FechaViaje { get; set; }
        public int NumeroViaje { get; set; }
        public string NumeroGuia { get; set; }
        public string Responsable { get; set; }
        public string PuntoPartida { get; set; }
        public string PuntoLlegada { get; set; }
        public string EstadoViaje { get; set; }
        public DateTime FechaCreacionViaje { get; set; }
        public DateTime? FechaModificacionViaje { get; set; }
        public string UsuarioCreacionViaje { get; set; }
        public string UsuarioModificacionViaje { get; set; }

        // Información de Transporte    
        public string NombreEmpresa { get; set; }
        public string RUCEmpresa { get; set; }
        public string NombreConductor { get; set; }
        public string PlacaVehiculo { get; set; }

        // ✅ NUEVOS CAMPOS PARA PALLETS BICOLOR
        public string SegundaVariedad { get; set; }
        public int? CajasSegundaVariedad { get; set; }
        public bool EsBicolor { get; set; }

        // ✅ PROPIEDADES CALCULADAS PARA CLASIFICACIÓN PC/PH  
        public string TipoPallet => DeterminarTipoPallet();
        public bool EsPC => TipoPallet == "PC";
        public bool EsPH => TipoPallet == "PH";  
        public bool EsCT => TipoPallet == "CT";
        public bool EsEN => TipoPallet == "EN";

        // ✅ PROPIEDADES PARA REPORTERÍA BICOLOR  
        public string VariedadParaReporte => EsBicolor && !string.IsNullOrEmpty(SegundaVariedad) ?
            $"{Variedad} + {SegundaVariedad}" : Variedad;

        public int CajasParaReporte => EsBicolor && CajasSegundaVariedad.HasValue ?
            NumeroDeCajas + CajasSegundaVariedad.Value : NumeroDeCajas;

        public decimal PesoTotalBicolor => EsBicolor && CajasSegundaVariedad.HasValue ?
            (NumeroDeCajas + CajasSegundaVariedad.Value) * PesoUnitario : PesoTotal;

        // ✅ PROPIEDADES PARA MOSTRAR DETALLES EN VENTANAS  
        public string TotalCajasDisplay => EsBicolor && CajasSegundaVariedad.HasValue ?
            $"{CajasParaReporte}" :
            //MUESTRA EN TOTAL DE CAJAS DE AMBAS VARIEDADES EN EL REPORTE
           // $"{CajasParaReporte} ({NumeroDeCajas}+{CajasSegundaVariedad})" :
            NumeroDeCajas.ToString();

        // ✅ MÉTODO PRIVADO PARA DETERMINAR TIPO DE PALLET  
        private string DeterminarTipoPallet()
        {
            if (NumeroPallet.ToUpper().EndsWith("PC") || NumeroPallet.ToUpper().Contains("PC"))
                return "PC";
            else if (NumeroPallet.ToUpper().EndsWith("PH") || NumeroPallet.ToUpper().Contains("PH"))
                return "PH";
            else if (NumeroPallet.ToUpper().EndsWith("CT") || NumeroPallet.ToUpper().Contains("CT"))
                return "CT";
            else if (NumeroPallet.ToUpper().EndsWith("EN") || NumeroPallet.ToUpper().Contains("EN"))
                return "EN";
            else
                return "PC"; // Por defecto PC si no se puede determinar  
        }
    }

}