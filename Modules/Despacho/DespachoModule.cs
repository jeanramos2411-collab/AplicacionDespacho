using System.Collections.Generic;
using System.Windows;
using AplicacionDespacho.Modules.Common.Base;
using AplicacionDespacho.Modules.Common.Models;
using AplicacionDespacho.Modules.Despacho.Views;

namespace AplicacionDespacho.Modules.Despacho
{
    /// <summary>    
    /// Módulo de Despacho - funcionalidad existente del sistema    
    /// </summary>    
    public class DespachoModule : ModuleBase
    {
        protected override ModuleInfo CreateModuleInfo()
        {
            return new ModuleInfo
            {
                ModuleId = "Despacho",
                DisplayName = "Despacho",
                Description = "Gestión de viajes y despacho de pallets",
                Icon = "🚚",
                DisplayOrder = 1,
                IsEnabled = true,
                AvailableProfiles = new List<string>() // Sin perfiles, es un módulo simple  
            };
        }

        protected override ModulePermissions CreatePermissions()
        {
            return new ModulePermissions
            {
                CanReadPackingSJP = true,
                CanWritePackingSJP = false,  // Solo lectura para Packing_SJP  
                CanReadDespachosSJP = true,
                CanWriteDespachosSJP = true  // Lectura/escritura para Despachos_SJP  
            };
        }

        protected override Window CreateMainWindow(string deviceId)
        {
            // Crear y retornar la ventana principal de Despacho  
            // El parámetro deviceId se ignora por ahora, pero está disponible si se necesita en el futuro  
            return new MainWindow();
        }
    }
}