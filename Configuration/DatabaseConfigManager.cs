using System;
using Microsoft.Win32;

namespace AplicacionDespacho.Configuration
{
    public static class DatabaseConfigManager
    {
        private const string REGISTRY_KEY = @"SOFTWARE\\AplicacionDespacho\\Database";

        public static void GuardarConfiguracion(string servidor, string usuario, string password, int timeout)
        {
            // Validaciones básicas  
            if (string.IsNullOrWhiteSpace(servidor))
                throw new ArgumentException("El servidor no puede estar vacío");

            if (string.IsNullOrWhiteSpace(usuario))
                throw new ArgumentException("El usuario no puede estar vacío");

            if (timeout <= 0)
                throw new ArgumentException("El timeout debe ser mayor que cero");

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    key.SetValue("Servidor", servidor);
                    key.SetValue("Usuario", usuario);
                    key.SetValue("Password", ProtegerPassword(password ?? ""));
                    key.SetValue("Timeout", timeout);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar configuración en registro: {ex.Message}");
            }
        }

        public static (string servidor, string usuario, string password, int timeout) CargarConfiguracion()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        string servidor = key.GetValue("Servidor")?.ToString() ?? "";
                        string usuario = key.GetValue("Usuario")?.ToString() ?? "";
                        string passwordEncriptado = key.GetValue("Password")?.ToString() ?? "";
                        string password = DesprotegerPassword(passwordEncriptado);

                        // Mejorar el parsing del timeout con validación  
                        int timeout = 30;
                        if (int.TryParse(key.GetValue("Timeout")?.ToString(), out int parsedTimeout) && parsedTimeout > 0)
                        {
                            timeout = parsedTimeout;
                        }

                        return (servidor, usuario, password, timeout);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar configuración: {ex.Message}");
            }

            return ("", "", "", 30);
        }

        public static bool ExisteConfiguracion()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Clave del registro: {key != null}");
                    if (key != null)
                    {
                        var servidor = key.GetValue("Servidor")?.ToString();
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Valor servidor: '{servidor}'");
                        return !string.IsNullOrEmpty(servidor);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error en ExisteConfiguracion: {ex.Message}");
                return false;
            }
        }

        public static void EliminarConfiguracion()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(REGISTRY_KEY, false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al eliminar configuración: {ex.Message}");
            }
        }

        private static string ProtegerPassword(string password)
        {
            try
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] encrypted = System.Security.Cryptography.ProtectedData.Protect(data, null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                // Fallback a Base64 si ProtectedData falla  
                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static string DesprotegerPassword(string passwordEncriptado)
        {
            try
            {
                byte[] encrypted = Convert.FromBase64String(passwordEncriptado);
                byte[] data = System.Security.Cryptography.ProtectedData.Unprotect(encrypted, null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(data);
            }
            catch
            {
                // Fallback a Base64 si ProtectedData falla  
                try
                {
                    return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(passwordEncriptado));
                }
                catch
                {
                    return "";
                }
            }
        }
    }
}