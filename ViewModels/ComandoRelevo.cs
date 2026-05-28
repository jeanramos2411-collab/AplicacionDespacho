// ViewModels/ComandoRelevo.cs  
using System;
using System.Windows.Input;

namespace AplicacionDespacho.ViewModels
{
    public class ComandoRelevo : ICommand
    {
        private readonly Action<object> _ejecutar;
        private readonly Func<object, bool> _puedeEjecutar;

        public ComandoRelevo(Action<object> ejecutar, Func<object, bool> puedeEjecutar = null)
        {
            _ejecutar = ejecutar ?? throw new ArgumentNullException(nameof(ejecutar));
            _puedeEjecutar = puedeEjecutar;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _puedeEjecutar?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _ejecutar(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}