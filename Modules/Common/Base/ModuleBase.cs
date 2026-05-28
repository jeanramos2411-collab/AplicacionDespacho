using System;
using System.Windows;
using AplicacionDespacho.Modules.Common.Interfaces;
using AplicacionDespacho.Modules.Common.Models;
using AplicacionDespacho.Services;

namespace AplicacionDespacho.Modules.Common.Base
{
    /// <summary>  
    /// Clase base abstracta para todos los módulos del sistema  
    /// Proporciona funcionalidad común y plantilla de implementación  
    /// </summary>  
    public abstract class ModuleBase : IModule
    {
        protected SignalRService SignalRService { get; set; }
        protected string DeviceId { get; set; }
        protected ModuleInfo ModuleInfo { get; set; }
        protected ModulePermissions Permissions { get; set; }

        protected ModuleBase()
        {
            ModuleInfo = CreateModuleInfo();
            Permissions = CreatePermissions();
        }

        /// <summary>  
        /// Método abstracto que cada módulo debe implementar para definir su información  
        /// </summary>  
        protected abstract ModuleInfo CreateModuleInfo();

        /// <summary>  
        /// Método abstracto que cada módulo debe implementar para definir sus permisos  
        /// </summary>  
        protected abstract ModulePermissions CreatePermissions();

        /// <summary>  
        /// Método abstracto que cada módulo debe implementar para crear su ventana principal  
        /// </summary>  
        protected abstract Window CreateMainWindow(string deviceId);

        public ModuleInfo GetModuleInfo() => ModuleInfo;

        public ModulePermissions GetRequiredPermissions() => Permissions;

        public virtual Window InitializeModule(string deviceId = null)
        {
            DeviceId = deviceId;

            // Validar permisos antes de inicializar  
            if (!ValidatePermissions())
            {
                throw new UnauthorizedAccessException(
                    $"El módulo {ModuleInfo.DisplayName} requiere permisos que no están disponibles.");
            }

            // Inicializar SignalR con prefijo del módulo  
            InitializeSignalR();

            // Crear y retornar ventana principal  
            return CreateMainWindow(deviceId);
        }

        public virtual bool ValidatePermissions()
        {
            // Aquí se validaría contra las credenciales actuales de la BD  
            // Por ahora, asumimos que la conexión de superusuario ya está configurada  
            return true;
        }

        protected virtual void InitializeSignalR()
        {
            // Inicializar SignalR con configuración del módulo  
            var hubUrl = Configuration.AppConfig.SignalRHubUrl;
            SignalRService = new SignalRService(hubUrl);
        }
        public virtual void Cleanup()
        {
            // Limpiar recursos del módulo    
            SignalRService?.StopConnectionAsync().Wait(); // ✅ CORRECTO  
        }
    }
}