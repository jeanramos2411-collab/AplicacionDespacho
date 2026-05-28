// Models/InformacionPallet.cs - Campos para bicolor con cantidad única de cajas  
using AplicacionDespacho.utilities;

namespace AplicacionDespacho.Models
{
    public class InformacionPallet
    {
        // Campos existentes (mantener todos)      
        public string NumeroPallet { get; set; }
        public string Variedad { get; set; }
        public string Calibre { get; set; }
        public string Embalaje { get; set; }
        public int NumeroDeCajas { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal PesoTotal { get; set; }
        public bool TienePesoInconsistente { get; set; } = false;

        // Campos para rastrear modificaciones (mantener todos)      
        public string VariedadOriginal { get; set; }
        public string CalibreOriginal { get; set; }
        public string EmbalajeOriginal { get; set; }
        public int NumeroDeCajasOriginal { get; set; }
        public bool Modificado { get; set; } = false;
        public DateTime FechaEscaneo { get; set; } = FechaOperacionalHelper.ObtenerFechaOperacionalActual();
        public DateTime? FechaModificacion { get; set; }

        // CAMPOS PARA PALLETS BICOLOR (mantener por compatibilidad BD)  
        public bool EsBicolor { get; set; } = false;
        public string SegundaVariedad { get; set; }
        public int CajasSegundaVariedad { get; set; } = 0;  // Ya no se usa en lógica, solo BD  

        // Campos para rastrear modificaciones bicolor    
        public string SegundaVariedadOriginal { get; set; }
        public int CajasSegundaVariedadOriginal { get; set; } = 0;

        // ✅ CAMBIO 1: TotalCajasBicolor ahora siempre retorna NumeroDeCajas  
        // Ya no suma CajasSegundaVariedad - bicolor usa cantidad única  
        public int TotalCajasBicolor => NumeroDeCajas;

        // ✅ CAMBIO 2: PesoTotalBicolor usa solo NumeroDeCajas  
        // Mismo cálculo para monocolor y bicolor  
        public decimal PesoTotalBicolor => NumeroDeCajas * PesoUnitario;

        // PROPIEDADES PARA CLASIFICACIÓN PC/PH/CT/EN (sin cambios)  
        public string TipoPallet => DeterminarTipoPallet();
        public bool EsPC => TipoPallet == "PC";
        public bool EsPH => TipoPallet == "PH";
        public bool EsCT => TipoPallet == "CT";
        public bool EsEN => TipoPallet == "EN";

        // ✅ CAMBIO 3: Propiedades para reportería - solo NumeroDeCajas  
        // VariedadParaReporte sigue mostrando ambas variedades (sin cambio)  
        public string VariedadParaReporte => EsBicolor ? $"{Variedad} + {SegundaVariedad}" : Variedad;

        // CajasParaReporte ahora usa solo NumeroDeCajas para bicolor  
        public int CajasParaReporte => NumeroDeCajas;

        // ✅ CAMBIO 4: DescripcionCompleta sin desglose de cajas por variedad  
        // Muestra ambas variedades pero una sola cantidad total    
        public string DescripcionCompleta => EsBicolor ?
            $"BICOLOR: {Variedad} + {SegundaVariedad} - {NumeroDeCajas} cajas" :
            $"{Variedad} - {NumeroDeCajas} cajas";

        // VariedadDisplay sigue mostrando ambas variedades (sin cambio)  
        public string VariedadDisplay => EsBicolor ?
            $"{Variedad} + {SegundaVariedad}" :
            Variedad;

        // ✅ CAMBIO 5: TotalCajasDisplay muestra solo el número sin desglose  
        // Ya no muestra formato "180 (90+90)" para bicolor  
        public string TotalCajasDisplay => NumeroDeCajas.ToString();

        // Método privado para determinar tipo de pallet (sin cambios)  
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