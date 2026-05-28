using System.Windows;
using AplicacionDespacho.Modules.Common.Models;
namespace AplicacionDespacho.Modules.Common.Interfaces
{
    /// <summary>  
    /// Interfaz base que todos los módulos del sistema deben implementar  
    /// </summary>  
    public interface IModule
    {
        /// <summary>  
        /// Información descriptiva del módulo  
        /// </summary>  
        ModuleInfo GetModuleInfo();

        /// <summary>  
        /// Permisos de base de datos requeridos por el módulo  
        /// </summary>  
        ModulePermissions GetRequiredPermissions();

        /// <summary>  
        /// Inicializa el módulo y retorna la ventana principal  
        /// </summary>  
        /// <param name="deviceId">ID del dispositivo móvil asociado (opcional)</param>  
        /// <returns>Ventana principal del módulo</returns>  
        Window InitializeModule(string deviceId = null);

        /// <summary>  
        /// Valida si el módulo puede ejecutarse con las credenciales actuales  
        /// </summary>  
        /// <returns>True si tiene permisos suficientes</returns>  
        bool ValidatePermissions();

        /// <summary>  
        /// Limpia recursos al cerrar el módulo  
        /// </summary>  
        void Cleanup();
    }
}