namespace AplicacionDespacho.Modules.Common.Models
{
    /// <summary>  
    /// Información descriptiva de un módulo del sistema  
    /// </summary>  
    public class ModuleInfo
    {
        /// <summary>  
        /// Identificador único del módulo (ej: "Despacho", "Trazabilidad")  
        /// </summary>  
        public string ModuleId { get; set; }

        /// <summary>  
        /// Nombre para mostrar en UI  
        /// </summary>  
        public string DisplayName { get; set; }

        /// <summary>  
        /// Descripción del módulo  
        /// </summary>  
        public string Description { get; set; }

        /// <summary>  
        /// Icono del módulo (ruta a recurso o emoji)  
        /// </summary>  
        public string Icon { get; set; }

        /// <summary>  
        /// Orden de visualización en el selector  
        /// </summary>  
        public int DisplayOrder { get; set; }

        /// <summary>  
        /// Indica si el módulo está habilitado  
        /// </summary>  
        public bool IsEnabled { get; set; }

        /// <summary>  
        /// Perfiles disponibles dentro del módulo (ej: "Testeador", "Registrador")  
        /// </summary>  
        public List<string> AvailableProfiles { get; set; }

        public ModuleInfo()
        {
            AvailableProfiles = new List<string>();
            IsEnabled = true;
        }
    }
}