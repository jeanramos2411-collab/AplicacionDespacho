// Services/DataAccess/AccesoDatosViajesEstado.cs  
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using AplicacionDespacho.Configuration;
using AplicacionDespacho.Services.Logging;

namespace AplicacionDespacho.Services.DataAccess
{
    public class AccesoDatosViajesEstado
    {
        private readonly ILoggingService _logger;
        private readonly string _clienteId;
        private readonly string _nombreCliente;

        public AccesoDatosViajesEstado()
        {
            _logger = LoggingFactory.CreateLogger("ViajesEstado");
            _clienteId = Environment.MachineName + "_" + Environment.UserName;
            _nombreCliente = $"{Environment.MachineName} ({Environment.UserName})";
        }

        public async Task<(bool exito, string mensaje)> MarcarViajeEnUsoAsync(string numeroGuia, int timeoutMinutos = 30)
        {
            try
            {
                using (var connection = new SqlConnection(AppConfig.GetConnectionString("DespachosSJP")))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("SP_MarcarViajeEnUso", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@NumeroGuia", numeroGuia);
                        command.Parameters.AddWithValue("@ClienteId", _clienteId);
                        command.Parameters.AddWithValue("@NombreCliente", _nombreCliente);
                        command.Parameters.AddWithValue("@TimeoutMinutos", timeoutMinutos);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var resultado = reader.GetInt32("Resultado");
                                var mensaje = reader.GetString("Mensaje");

                                _logger.LogInfo("Resultado marcar viaje {NumeroGuia}: {Resultado} - {Mensaje}",
                                              numeroGuia, resultado, mensaje);

                                return (resultado == 1, mensaje);
                            }
                        }
                    }
                }

                return (false, "No se pudo procesar la solicitud");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando viaje como en uso: {NumeroGuia}", numeroGuia);
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<bool> LiberarViajeAsync(string numeroGuia)
        {
            try
            {
                using (var connection = new SqlConnection(AppConfig.GetConnectionString("DespachosSJP")))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("SP_LiberarViaje", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@NumeroGuia", numeroGuia);
                        command.Parameters.AddWithValue("@ClienteId", _clienteId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var filasAfectadas = reader.GetInt32("FilasAfectadas");
                                _logger.LogInfo("Viaje liberado {NumeroGuia}: {FilasAfectadas} filas afectadas",
                                              numeroGuia, filasAfectadas);
                                return filasAfectadas > 0;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liberando viaje: {NumeroGuia}", numeroGuia);
                return false;
            }
        }

        public async Task<Dictionary<string, bool>> ObtenerEstadoViajesAsync()
        {
            var estadoViajes = new Dictionary<string, bool>();

            try
            {
                using (var connection = new SqlConnection(AppConfig.GetConnectionString("DespachosSJP")))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("SP_ObtenerEstadoViajes", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var numeroGuia = reader.GetString("NumeroGuia");
                                var enUso = reader.GetBoolean("EnUso");
                                estadoViajes[numeroGuia] = enUso;
                            }
                        }
                    }
                }

                _logger.LogInfo("Estado de viajes obtenido: {Count} viajes en uso", estadoViajes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado de viajes");
            }

            return estadoViajes;
        }

        public async Task<bool> ActualizarHeartbeatAsync(string numeroGuia)
        {
            try
            {
                using (var connection = new SqlConnection(AppConfig.GetConnectionString("DespachosSJP")))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("SP_ActualizarHeartbeat", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@NumeroGuia", numeroGuia);
                        command.Parameters.AddWithValue("@ClienteId", _clienteId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var actualizado = reader.GetInt32("Actualizado");
                                return actualizado > 0;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando heartbeat: {NumeroGuia}", numeroGuia);
                return false;
            }
        }
    }
}
