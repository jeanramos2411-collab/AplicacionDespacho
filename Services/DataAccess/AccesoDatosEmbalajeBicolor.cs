using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using AplicacionDespacho.Configuration;

namespace AplicacionDespacho.Services.DataAccess
{
    public class AccesoDatosEmbalajeBicolor
    {
        private readonly string _cadenaConexion;

        public AccesoDatosEmbalajeBicolor()
        {
            _cadenaConexion = AppConfig.DespachosSJPConnectionStringDynamic;
        }

        public List<string> ObtenerEmbalajeBicolorActivos()
        {
            var embalajes = new List<string>();
            string consulta = @"  
                SELECT CodigoEmbalaje   
                FROM EMBALAJES_BICOLOR   
                WHERE Activo = 1";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();
                        while (lector.Read())
                        {
                            embalajes.Add(lector["CodigoEmbalaje"].ToString());
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener embalajes bicolor: {ex.Message}");
                        throw;
                    }
                }
            }

            return embalajes;
        }

        public bool EsEmbalajeBicolor(string codigoEmbalaje)
        {
            var embalajesActivos = ObtenerEmbalajeBicolorActivos();
            return embalajesActivos.Contains(codigoEmbalaje);
        }
    }
}