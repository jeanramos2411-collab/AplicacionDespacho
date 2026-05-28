namespace AplicacionDespacho.Modules.Common.Models
{
    /// <summary>  
    /// Define los permisos de acceso a bases de datos requeridos por un módulo  
    /// </summary>  
    public class ModulePermissions
    {
        /// <summary>  
        /// Puede leer de Packing_SJP  
        /// </summary>  
        public bool CanReadPackingSJP { get; set; }

        /// <summary>  
        /// Puede escribir/eliminar en Packing_SJP (solo para Testing/Trazabilidad)  
        /// </summary>  
        public bool CanWritePackingSJP { get; set; }

        /// <summary>  
        /// Puede leer de Despachos_SJP  
        /// </summary>  
        public bool CanReadDespachosSJP { get; set; }

        /// <summary>  
        /// Puede escribir en Despachos_SJP  
        /// </summary>  
        public bool CanWriteDespachosSJP { get; set; }

        /// <summary>  
        /// Requiere conexión de superusuario a SQL Server  
        /// </summary>  
        public bool RequiresSuperUser { get; set; }

        public ModulePermissions()
        {
            // Por defecto, solo lectura en Packing_SJP  
            CanReadPackingSJP = true;
            CanWritePackingSJP = false;
            CanReadDespachosSJP = true;
            CanWriteDespachosSJP = false;
            RequiresSuperUser = false;
        }
    }
}