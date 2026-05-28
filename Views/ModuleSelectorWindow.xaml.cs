using AplicacionDespacho.Modules.Common.Interfaces;
using AplicacionDespacho.Modules.Common.Models;
using AplicacionDespacho.Modules.Common.Views;
using AplicacionDespacho.Modules.Despacho;
using AplicacionDespacho.Modules.Trazabilidad;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace AplicacionDespacho.Views
{
    public partial class ModuleSelectorWindow : Window
    {
        private List<IModule> _availableModules;

        public ModuleSelectorWindow()
        {
            InitializeComponent();
            LoadAvailableModules();
        }

        private void LoadAvailableModules()
        {
            _availableModules = new List<IModule>
            {
                new DespachoModule(),
                new TrazabilidadModule() // Sin perfil por defecto - se seleccionará en la UI  
            };

            // Filtrar solo módulos habilitados y ordenar        
            var enabledModules = _availableModules
                .Where(m => m.GetModuleInfo().IsEnabled)
                .OrderBy(m => m.GetModuleInfo().DisplayOrder)
                .Select(m => m.GetModuleInfo())
                .ToList();

            ModulesItemsControl.ItemsSource = enabledModules;
        }

        // Método para abrir módulo sin perfil específico (para módulos simples como Despacho)  
        private void OpenModule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ModuleInfo moduleInfo)
            {
                var module = _availableModules.FirstOrDefault(m => m.GetModuleInfo().ModuleId == moduleInfo.ModuleId);
                if (module != null)
                {
                    LaunchModule(module, null);
                }
            }
        }

        // Método para hacer clic en la tarjeta del módulo (solo para módulos sin perfiles)  
        private void ModuleCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ModuleInfo moduleInfo)
            {
                // Solo lanzar si el módulo NO tiene perfiles  
                if (moduleInfo.AvailableProfiles == null || moduleInfo.AvailableProfiles.Count == 0)
                {
                    var module = _availableModules.FirstOrDefault(m => m.GetModuleInfo().ModuleId == moduleInfo.ModuleId);
                    if (module != null)
                    {
                        LaunchModule(module, null);
                    }
                }
            }
        }

        // Método para abrir módulo con perfil específico (para módulos con perfiles como Trazabilidad)  
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // El Content del botón es el nombre del perfil (string)  
                string profile = button.Content.ToString();

                // Navegar por el árbol visual para encontrar el Border padre que contiene el ModuleInfo  
                DependencyObject parent = VisualTreeHelper.GetParent(button);

                // Subir hasta encontrar el Border principal de la tarjeta del módulo  
                while (parent != null)
                {
                    if (parent is Border border && border.DataContext is ModuleInfo moduleInfo)
                    {
                        string moduleId = moduleInfo.ModuleId;

                        // Crear NUEVA instancia del módulo con el perfil específico  
                        IModule module = null;
                        if (moduleId == "Trazabilidad")
                        {
                            module = new TrazabilidadModule(profile);
                        }
                        // Aquí se pueden agregar más módulos con perfiles en el futuro  
                        // else if (moduleId == "OtroModulo") { ... }  

                        if (module != null)
                        {
                            LaunchModule(module, null);
                        }

                        return; // Salir después de encontrar y procesar  
                    }

                    parent = VisualTreeHelper.GetParent(parent);
                }

                // Si llegamos aquí, no se encontró el ModuleInfo  
                MessageBox.Show(
                    "No se pudo determinar el módulo para el perfil seleccionado.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LaunchModule(IModule module, string profile)
        {
            try
            {
                // Nota: El perfil ya está configurado en el constructor del módulo  
                // No es necesario llamar a SwitchProfile aquí  

                // Inicializar el módulo  
                var moduleWindow = module.InitializeModule();

                // Ocultar selector  
                this.Hide();

                // Mostrar ventana del módulo (modal)  
                moduleWindow.ShowDialog();

                // Al cerrar, volver a mostrar selector  
                this.Show();

                // Limpiar recursos  
                module.Cleanup();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al iniciar el módulo {module.GetModuleInfo().DisplayName}:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ConfigurarBaseDatos_Click(object sender, RoutedEventArgs e)
        {
            var ventanaConfig = new ConfiguracionBaseDatosWindow();
            ventanaConfig.Owner = this;
            ventanaConfig.ShowDialog();
        }

        private void ConfigurarSignalR_Click(object sender, RoutedEventArgs e)
        {
            var ventanaConfig = new ConfiguracionSignalRWindow();
            ventanaConfig.Owner = this;
            ventanaConfig.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Está seguro que desea salir del sistema?",
                "Confirmar Salida",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }

    // Convertidor para mostrar elemento cuando Count > 0  
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string> list)
            {
                return list.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Convertidor inverso para mostrar elemento cuando Count == 0  
    public class InverseCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string> list)
            {
                return list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible; // Por defecto, mostrar si no hay lista  
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}