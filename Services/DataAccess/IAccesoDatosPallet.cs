// Services/DataAccess/IAccesoDatosPallet.cs
using AplicacionDespacho.Models;

namespace AplicacionDespacho.Services.DataAccess
{
    public interface IAccesoDatosPallet
    {
        InformacionPallet ObtenerDatosPallet(string numeroPallet);
    }
}