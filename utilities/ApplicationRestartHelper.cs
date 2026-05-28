// utilities/ApplicationRestartHelper.cs  
using System;
using System.Diagnostics;
using System.Windows;
using AplicacionDespacho.Services.Logging;

namespace AplicacionDespacho.utilities
{
    public static class ApplicationRestartHelper
    {
        private static readonly ILoggingService _logger = LoggingFactory.CreateLogger("ApplicationRestart");

        public static bool PromptForRestart(string configurationChanged, Window owner = null)
        {
            var message = $"La configuración de {configurationChanged} ha sido actualizada.\\n\\n" +
                         "¿Desea reiniciar la aplicación ahora para aplicar los cambios?\\n\\n" +
                         "Si selecciona 'No', los cambios se aplicarán en el próximo inicio.";

            var result = MessageBox.Show(message,
                                       "Reinicio Requerido",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        public static void RestartApplication()
        {
            try
            {
                _logger.LogInfo("Iniciando reinicio de aplicación");

                // Obtener la ruta del ejecutable actual  
                var currentExecutable = Process.GetCurrentProcess().MainModule.FileName;

                // Iniciar nueva instancia  
                Process.Start(currentExecutable);

                _logger.LogInfo("Nueva instancia iniciada, cerrando aplicación actual");

                // Cerrar aplicación actual  
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el reinicio de la aplicación");
                MessageBox.Show($"Error al reiniciar la aplicación: {ex.Message}\\n\\n" +
                               "Por favor, reinicie manualmente la aplicación.",
                               "Error de Reinicio",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        public static void PromptAndRestartIfConfirmed(string configurationChanged, Window owner = null)
        {
            if (PromptForRestart(configurationChanged, owner))
            {
                RestartApplication();
            }
        }
    }
}