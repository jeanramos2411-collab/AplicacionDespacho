using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AplicacionDespacho.utilities
{
    public static class FechaOperacionalHelper
    {
        private const int HORA_CORTE_OPERACIONAL = 8; // 8:00 AM  

        /// <summary>  
        /// Calcula la fecha operacional basada en la hora de corte de 8:00 AM  
        /// Si la hora actual es antes de las 8:00 AM, retorna el día anterior  
        /// Si es 8:00 AM o después, retorna el día actual  
        /// </summary>  
        /// <param name="fechaHoraActual">Fecha y hora a evaluar</param>  
        /// <returns>Fecha operacional (solo fecha, sin hora)</returns>  
        /// 

        /* public static DateTime CalcularFechaOperacional(DateTime fechaHoraActual)
         {
             var fechaOperacional = fechaHoraActual.Hour < HORA_CORTE_OPERACIONAL
                 ? fechaHoraActual.Date.AddDays(-1)
                 : fechaHoraActual.Date;

             // Log para auditoría cuando se aplica la lógica de día anterior  
             if (fechaOperacional != fechaHoraActual.Date)
             {
                 System.Diagnostics.Debug.WriteLine(
                     $"[FechaOperacional] Aplicada lógica de corte: {fechaHoraActual:yyyy-MM-dd HH:mm:ss} -> {fechaOperacional:yyyy-MM-dd}");
             }

             return fechaOperacional;
         }*/
        public static DateTime CalcularFechaOperacional(DateTime fechaHoraActual)
        {
            if (fechaHoraActual.Hour < HORA_CORTE_OPERACIONAL)
            {
                // Solo en horario temprano: fecha del día anterior a las 00:00:00  
                var fechaOperacional = fechaHoraActual.Date.AddDays(-1);

                System.Diagnostics.Debug.WriteLine(
                    $"[FechaOperacional] Aplicada lógica de corte: {fechaHoraActual:yyyy-MM-dd HH:mm:ss} -> {fechaOperacional:yyyy-MM-dd}");

                return fechaOperacional;
            }
            else
            {
                // Después de las 8:00 AM: mantener fecha y hora actual  
                return fechaHoraActual;
            }
        }

        /// <summary>  
        /// Obtiene la fecha operacional actual basada en DateTime.Now  
        /// </summary>  
        /// <returns>Fecha operacional actual</returns>  
        public static DateTime ObtenerFechaOperacionalActual()
        {
            return CalcularFechaOperacional(DateTime.Now);
        }

        /// <summary>  
        /// Verifica si una fecha/hora específica aplicaría la lógica de día anterior  
        /// </summary>  
        /// <param name="fechaHora">Fecha y hora a verificar</param>  
        /// <returns>True si se aplicaría la lógica de día anterior</returns>  
        public static bool AplicaLogicaDiaAnterior(DateTime fechaHora)
        {
            return fechaHora.Hour < HORA_CORTE_OPERACIONAL;
        }

        /// <summary>  
        /// Obtiene un mensaje descriptivo de la fecha operacional aplicada  
        /// </summary>  
        /// <param name="fechaHoraOriginal">Fecha y hora original</param>  
        /// <returns>Mensaje descriptivo</returns>  
        public static string ObtenerMensajeDescriptivo(DateTime fechaHoraOriginal)
        {
            var fechaOperacional = CalcularFechaOperacional(fechaHoraOriginal);

            if (fechaOperacional != fechaHoraOriginal.Date)
            {
                return $"Fecha operacional: {fechaOperacional:dd/MM/yyyy} (aplicada lógica de corte por ser antes de las {HORA_CORTE_OPERACIONAL}:00 AM)";
            }

            return $"Fecha operacional: {fechaOperacional:dd/MM/yyyy}";
        }
    }
}
