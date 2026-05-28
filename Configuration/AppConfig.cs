// Configuration/AppConfig.cs  ESCRITORIO          
using System;
using System.Configuration;
using System.Data.SqlClient;
using AplicacionDespacho.Services.Logging;
namespace AplicacionDespacho.Configuration
{
    public static class AppConfig
    {
        // Cadenas de conexión            
        public static string PackingSJPConnectionString =>
            ConfigurationManager.ConnectionStrings["PackingSJP"]?.ConnectionString
            ?? throw new InvalidOperationException("Cadena de conexión PackingSJP no encontrada");

        public static string DespachosSJPConnectionString =>
            ConfigurationManager.ConnectionStrings["DespachosSJP"]?.ConnectionString
            ?? throw new InvalidOperationException("Cadena de conexión DespachosSJP no encontrada");

        // Configuración dinámica de base de datos con fallback    
        public static string PackingSJPConnectionStringDynamic =>
            GetDynamicConnectionString("PackingSJP") ?? PackingSJPConnectionString;

        public static string DespachosSJPConnectionStringDynamic =>
            GetDynamicConnectionString("DespachosSJP") ?? DespachosSJPConnectionString;

        // MODIFICADO: Configuración de SignalR con valor por defecto siguiendo el patrón existente    
        public static string SignalRHubUrl =>
            ConfigurationManager.AppSettings["SignalRHubUrl"] ?? GetDefaultSignalRHubUrl();

        // Configuraciones de negocio            
        public static int MaxPalletsPerTrip =>
            int.TryParse(ConfigurationManager.AppSettings["MaxPalletsPerTrip"], out int value) ? value : 50;

        public static string DefaultGuidePrefix =>
            ConfigurationManager.AppSettings["DefaultGuidePrefix"] ?? "T004-";

        public static string DefaultResponsible =>
            ConfigurationManager.AppSettings["DefaultResponsible"] ?? "MIRTHA INGA";

        public static string DefaultDeparturePoint =>
            ConfigurationManager.AppSettings["DefaultDeparturePoint"] ?? "PIURA";

        public static string DefaultArrivalPoint =>
            ConfigurationManager.AppSettings["DefaultArrivalPoint"] ?? "SULLANA";

        // Configuraciones de logging            
        public static string LogLevel =>
            ConfigurationManager.AppSettings["LogLevel"] ?? "Info";

        public static string LogFilePath =>
            ConfigurationManager.AppSettings["LogFilePath"] ?? "Logs\\\\AplicacionDespacho.log";

        public static bool EnableFileLogging =>
            bool.TryParse(ConfigurationManager.AppSettings["EnableFileLogging"], out bool value) && value;

        // Configuraciones de UI            
        public static int DataGridPageSize =>
            int.TryParse(ConfigurationManager.AppSettings["DataGridPageSize"], out int value) ? value : 100;

        public static int AutoSaveInterval =>
            int.TryParse(ConfigurationManager.AppSettings["AutoSaveInterval"], out int value) ? value : 300;

        public static bool ShowDebugInfo =>
            bool.TryParse(ConfigurationManager.AppSettings["ShowDebugInfo"], out bool value) && value;

        // Cache para cadenas de conexión 
        private static readonly Dictionary<string, string> _connectionStringCache = new();
        private static readonly object _cacheLock = new object();
        private static DateTime _lastCacheUpdate = DateTime.MinValue;
        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        // NUEVO: Método para establecer URL de SignalR          
        public static void SetSignalRHubUrl(string hubUrl)
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                // Remover configuración existente si existe          
                if (config.AppSettings.Settings["SignalRHubUrl"] != null)
                {
                    config.AppSettings.Settings.Remove("SignalRHubUrl");
                }

                // Agregar nueva configuración          
                config.AppSettings.Settings.Add("SignalRHubUrl", hubUrl);

                // Guardar cambios          
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al guardar configuración SignalR: {ex.Message}", ex);
            }
        }

        // NUEVO: Método para validar URL de SignalR          
        public static bool IsValidSignalRUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                var uri = new Uri(url);
                return uri.Scheme == "http" || uri.Scheme == "https";
            }
            catch
            {
                return false;
            }
        }

        // CORREGIDO: Método para obtener URL por defecto de SignalR sin hardcodeo    
        public static string GetDefaultSignalRHubUrl()
        {
            // CAMBIO PRINCIPAL: Usar localhost por defecto en lugar de IP específica    
            return "http://127.0.0.1:7164/pallethub";
        }

        // NUEVO: Método para restablecer configuración SignalR a valor por defecto          
        public static void ResetSignalRHubUrl()
        {
            SetSignalRHubUrl(GetDefaultSignalRHubUrl());
        }

        // NUEVO: Método para obtener URL actual o por defecto de SignalR        
        public static string GetSignalRHubUrlOrDefault()
        {
            var configuredUrl = ConfigurationManager.AppSettings["SignalRHubUrl"];
            return !string.IsNullOrEmpty(configuredUrl) ? configuredUrl : GetDefaultSignalRHubUrl();
        }

        // NUEVO: Método para verificar si SignalR está configurado        
        public static bool IsSignalRConfigured()
        {
            return !string.IsNullOrEmpty(ConfigurationManager.AppSettings["SignalRHubUrl"]);
        }

        // MEJORADO: Método para validar configuración al inicio            
        public static void ValidateConfiguration()
        {
            try
            {
                var _ = PackingSJPConnectionStringDynamic;  // Cambiar aquí  
                var __ = DespachosSJPConnectionStringDynamic;  // Cambiar aquí  

                // Validar configuración SignalR si existe  
                var signalRUrl = SignalRHubUrl;
                if (!string.IsNullOrEmpty(signalRUrl) && !IsValidSignalRUrl(signalRUrl))
                {
                    throw new ConfigurationErrorsException($"URL de SignalR inválida: {signalRUrl}");
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationErrorsException($"Error en configuración: {ex.Message}", ex);
            }
        }

        // NUEVO: Método para obtener toda la configuración SignalR          
        public static SignalRConfiguration GetSignalRConfiguration()
        {
            var configuredUrl = ConfigurationManager.AppSettings["SignalRHubUrl"];
            var effectiveUrl = SignalRHubUrl; // Esto incluye el fallback al valor por defecto        

            return new SignalRConfiguration
            {
                HubUrl = effectiveUrl,
                IsConfigured = !string.IsNullOrEmpty(configuredUrl),
                IsValid = IsValidSignalRUrl(effectiveUrl),
                IsDefault = string.IsNullOrEmpty(configuredUrl)
            };
        }

        // NUEVO: Método para construir URL completa desde IP y Puerto        
        public static string BuildSignalRUrl(string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentException("IP no puede estar vacía", nameof(ip));

            if (port <= 0 || port > 65535)
                throw new ArgumentException("Puerto debe estar entre 1 y 65535", nameof(port));

            return $"http://{ip}:{port}/pallethub";
        }

        // NUEVO: Método para extraer IP y Puerto de una URL SignalR        
        public static (string ip, int port) ParseSignalRUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL no puede estar vacía", nameof(url));

            try
            {
                var uri = new Uri(url);
                var ip = uri.Host;
                var port = uri.Port;

                return (ip, port);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"URL inválida: {ex.Message}", nameof(url));
            }
        }

        // NUEVO: Método para configurar SignalR desde IP y Puerto    
        public static void SetSignalRFromIpAndPort(string ip, int port)
        {
            var url = BuildSignalRUrl(ip, port);
            SetSignalRHubUrl(url);
        }

        // NUEVO: Método para obtener configuración actual como IP y Puerto    
        public static (string ip, int port) GetCurrentSignalRIpAndPort()
        {
            var currentUrl = SignalRHubUrl;
            return ParseSignalRUrl(currentUrl);
        }

        // Método para obtener configuración dinámica desde registro    
        private static string GetDynamicConnectionString(string databaseName)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Solicitando conexión dinámica para: {databaseName}");

            if (DatabaseConfigManager.ExisteConfiguracion())
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Configuración dinámica encontrada");
                var config = DatabaseConfigManager.CargarConfiguracion();

                // CORRECCIÓN: Mapear nombres de configuración a nombres reales de BD  
                string realDatabaseName = databaseName == "PackingSJP" ? "Packing_SJP" : "Despachos_SJP";
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mapeando {databaseName} -> {realDatabaseName}");

                var connectionString = BuildConnectionString(config.servidor, realDatabaseName, config.usuario, config.password, config.timeout);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Cadena construida: {connectionString}");
                return connectionString;
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] No hay configuración dinámica, usando fallback");
            return null;
        }
        // Construir cadena de conexión    
        public static string BuildConnectionString(string servidor, string database, string usuario, string password, int timeout)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = servidor,
                InitialCatalog = database,
                UserID = usuario,
                Password = password,
                ConnectTimeout = timeout // Usar ConnectTimeout (correcto)  
            };

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Cadena construida: {builder.ConnectionString}");
            return builder.ConnectionString;
        }
        public static string GetConnectionString(string databaseName)
        {
            lock (_cacheLock)
            {
                // Verificar si el cache está vigente  
                if (DateTime.Now - _lastCacheUpdate > _cacheExpiry)
                {
                    _connectionStringCache.Clear();
                    _lastCacheUpdate = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine("[DEBUG] 🔄 Cache de configuración limpiado");
                }

                // Verificar cache primero  
                if (_connectionStringCache.TryGetValue(databaseName, out string cachedConnection))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ✅ Usando conexión desde cache: {databaseName}");
                    return cachedConnection;
                }

                // Si no está en cache, obtener y cachear  
                string connectionString = GetConnectionStringInternal(databaseName);
                _connectionStringCache[databaseName] = connectionString;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 💾 Conexión cacheada: {databaseName}");

                return connectionString;
            }
        }
        private static string GetConnectionStringInternal(string databaseName)
        {
            switch (databaseName.ToUpper())
            {
                case "PACKINGSJP":
                    return PackingSJPConnectionStringDynamic;
                case "DESPACHOSSJP":
                    return DespachosSJPConnectionStringDynamic;
                default:
                    throw new ArgumentException($"Base de datos no reconocida: {databaseName}", nameof(databaseName));
            }
        }
        // Método para limpiar cache manualmente si es necesario  
        public static void ClearConnectionStringCache()
        {
            lock (_cacheLock)
            {
                _connectionStringCache.Clear();
                _lastCacheUpdate = DateTime.MinValue;
                System.Diagnostics.Debug.WriteLine("[DEBUG] 🧹 Cache de configuración limpiado manualmente");
            }
        }
        // Validar conexión de base de datos    
        public static bool IsValidDatabaseConnection(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Configurar base de datos desde parámetros    
        public static void SetDatabaseConfiguration(string servidor, string usuario, string password, int timeout)
        {
            DatabaseConfigManager.GuardarConfiguracion(servidor, usuario, password, timeout);
        }

        // Obtener configuración completa de base de datos    
        public static DatabaseConfiguration GetDatabaseConfiguration()
        {
            var (servidor, usuario, password, timeout) = DatabaseConfigManager.CargarConfiguracion();
            var isConfigured = DatabaseConfigManager.ExisteConfiguracion();

            return new DatabaseConfiguration
            {
                Servidor = servidor,
                Usuario = usuario,
                Timeout = timeout,
                IsConfigured = isConfigured,
                IsValid = !string.IsNullOrEmpty(servidor) && !string.IsNullOrEmpty(usuario)
            };
        }
    }

    // MODIFICADO: Clase para encapsular configuración SignalR con más propiedades        
    public class SignalRConfiguration
    {
        public string HubUrl { get; set; }
        public bool IsConfigured { get; set; }
        public bool IsValid { get; set; }
        public bool IsDefault { get; set; }

        // NUEVO: Propiedades adicionales para facilitar el uso        
        public string DisplayText => IsDefault ? $"{HubUrl} (Por defecto)" : HubUrl;
        public string StatusText => IsValid ? "Válida" : "Inválida";

        // NUEVO: Propiedades para obtener IP y Puerto por separado    
        public string IpAddress
        {
            get
            {
                try
                {
                    var (ip, _) = AppConfig.ParseSignalRUrl(HubUrl);
                    return ip;
                }
                catch
                {
                    return "N/A";
                }
            }
        }

        public int Port
        {
            get
            {
                try
                {
                    var (_, port) = AppConfig.ParseSignalRUrl(HubUrl);
                    return port;
                }
                catch
                {
                    return 0;
                }
            }
        }

    }

    public class DatabaseConfiguration
    {
        public string Servidor { get; set; }
        public string Usuario { get; set; }
        public int Timeout { get; set; }
        public bool IsConfigured { get; set; }
        public bool IsValid { get; set; }

        public string DisplayText => IsConfigured ? $"{Servidor} (Configurado)" : "Configuración por defecto";
        public string StatusText => IsValid ? "Válida" : "Inválida";
    }
}