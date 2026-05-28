using System.Windows;
using AplicacionDespacho.Modules.Trazabilidad.Profiles.Registrador.ViewModels;

namespace AplicacionDespacho.Modules.Trazabilidad.Profiles.Registrador.Views
{
    public partial class RegistradorWindow : Window
    {
        public RegistradorWindow()
        {
            InitializeComponent();
            this.DataContext = new RegistradorViewModel();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}