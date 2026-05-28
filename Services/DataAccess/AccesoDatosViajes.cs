// Services/DataAccess/AccesoDatosViajes.cs  
using AplicacionDespacho.Configuration;
using AplicacionDespacho.Models;
using AplicacionDespacho.Models.Reports;
using AplicacionDespacho.Services.Logging;
using AplicacionDespacho.utilities;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AplicacionDespacho.Services.DataAccess
{
    public class AccesoDatosViajes
    {
        private readonly string _cadenaConexion;
        private readonly ILoggingService _logger;
        private static int _instanceCount = 0;
        private readonly AccesoDatosEmbalajeBicolor _accesoDatosEmbalajeBicolor;

        public AccesoDatosViajes()
        {
            _cadenaConexion = AppConfig.DespachosSJPConnectionStringDynamic;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] AccesoDatosViajes usando conexión: {_cadenaConexion}");
            _logger = LoggingFactory.CreateLogger("AccesoDatosViajes"); // AGREGAR ESTA LÍNEA 
            _accesoDatosEmbalajeBicolor = new AccesoDatosEmbalajeBicolor();

            var instanceNumber = Interlocked.Increment(ref _instanceCount);
            _logger.LogInfo($"🏗️ AccesoDatosViajes instancia #{instanceNumber} creada");

            System.Diagnostics.Debug.WriteLine($"[DEBUG] 📊 Total instancias AccesoDatosViajes: {_instanceCount}");
        }

        public AccesoDatosViajes(string connectionString)
        {
            _cadenaConexion = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = LoggingFactory.CreateLogger("AccesoDatosViajes"); // AGREGAR ESTA LÍNEA  
        }

        public void GuardarViaje(Viaje nuevoViaje)
        {
            // Aplicar lógica de fecha operacional para FechaCreacion  
            var fechaOperacional = FechaOperacionalHelper.ObtenerFechaOperacionalActual();

            string consulta = @"  
                INSERT INTO VIAJES (Fecha, NumeroViaje, Responsable, NumeroGuia, PuntoPartida, PuntoLlegada, VehiculoId, ConductorId, Estado, FechaCreacion, UsuarioCreacion)  
                VALUES (@Fecha, @NumeroViaje, @Responsable, @NumeroGuia, @PuntoPartida, @PuntoLlegada, @VehiculoId, @ConductorId, @Estado, @FechaCreacion, @UsuarioCreacion);  
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@Fecha", nuevoViaje.Fecha);
                    comando.Parameters.AddWithValue("@NumeroViaje", nuevoViaje.NumeroViaje);
                    comando.Parameters.AddWithValue("@Responsable", nuevoViaje.Responsable);
                    comando.Parameters.AddWithValue("@NumeroGuia", nuevoViaje.NumeroGuia);
                    comando.Parameters.AddWithValue("@PuntoPartida", nuevoViaje.PuntoPartida ?? "");
                    comando.Parameters.AddWithValue("@PuntoLlegada", nuevoViaje.PuntoLlegada ?? "");
                    comando.Parameters.AddWithValue("@VehiculoId", nuevoViaje.VehiculoId);
                    comando.Parameters.AddWithValue("@ConductorId", nuevoViaje.ConductorId);
                    comando.Parameters.AddWithValue("@Estado", nuevoViaje.Estado ?? "Activo");
                    comando.Parameters.AddWithValue("@FechaCreacion", fechaOperacional); // CAMBIO AQUÍ
                    comando.Parameters.AddWithValue("@UsuarioCreacion", nuevoViaje.UsuarioCreacion ?? "");

                    try
                    {
                        conexion.Open();
                        var viajeId = comando.ExecuteScalar();
                        nuevoViaje.ViajeId = Convert.ToInt32(viajeId);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al guardar el viaje: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        // MÉTODOS DE VALIDACIÓN PARA LAS NUEVAS REGLAS DE NEGOCIO  
        public bool ExisteNumeroViajePorFecha(int numeroViaje, DateTime fecha, int? viajeIdExcluir = null)
        {
            string consulta = @"  
                SELECT COUNT(*) FROM VIAJES   
                WHERE NumeroViaje = @NumeroViaje   
                AND CAST(Fecha AS DATE) = CAST(@Fecha AS DATE)";

            if (viajeIdExcluir.HasValue)
            {
                consulta += " AND ViajeId != @ViajeIdExcluir";
            }

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NumeroViaje", numeroViaje);
                    comando.Parameters.AddWithValue("@Fecha", fecha.Date);
                    if (viajeIdExcluir.HasValue)
                    {
                        comando.Parameters.AddWithValue("@ViajeIdExcluir", viajeIdExcluir.Value);
                    }

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar número de viaje: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public bool ExisteNumeroGuia(string numeroGuia, int? viajeIdExcluir = null)
        {
            string consulta = "SELECT COUNT(*) FROM VIAJES WHERE NumeroGuia = @NumeroGuia";

            if (viajeIdExcluir.HasValue)
            {
                consulta += " AND ViajeId != @ViajeIdExcluir";
            }

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NumeroGuia", numeroGuia);
                    if (viajeIdExcluir.HasValue)
                    {
                        comando.Parameters.AddWithValue("@ViajeIdExcluir", viajeIdExcluir.Value);
                    }

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar número de guía: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public bool PalletYaFueEnviado(string numeroPallet)
        {
            string consulta = @"SELECT COUNT(*) FROM PALLETS_VIAJE pv    
                    INNER JOIN VIAJES v ON pv.ViajeId = v.ViajeId    
                    WHERE pv.NumeroPallet = @NumeroPallet     
                    AND (v.Estado = 'Finalizado' OR v.Estado = 'Activo')";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NumeroPallet", numeroPallet);

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar pallet enviado: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        public int ObtenerProximoNumeroViaje(DateTime fecha)
        {
            string consulta = @"  
        SELECT ISNULL(MAX(NumeroViaje), 0) + 1     
        FROM VIAJES     
        WHERE CAST(Fecha AS DATE) = CAST(@Fecha AS DATE)";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@Fecha", fecha.Date);

                    try
                    {
                        conexion.Open();
                        return (int)comando.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener próximo número de viaje: {ex.Message}");
                        return 1; // Valor por defecto si hay error  
                    }
                }
            }
        }
        public void ActualizarViaje(Viaje viaje)
        {
            string consulta = @"  
                UPDATE VIAJES SET   
                    Fecha = @Fecha,  
                    NumeroViaje = @NumeroViaje,  
                    Responsable = @Responsable,  
                    NumeroGuia = @NumeroGuia,  
                    PuntoPartida = @PuntoPartida,  
                    PuntoLlegada = @PuntoLlegada,  
                    VehiculoId = @VehiculoId,  
                    ConductorId = @ConductorId,  
                    Estado = @Estado,  
                    FechaModificacion = @FechaModificacion,  
                    UsuarioModificacion = @UsuarioModificacion  
                WHERE ViajeId = @ViajeId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@ViajeId", viaje.ViajeId);
                    comando.Parameters.AddWithValue("@Fecha", viaje.Fecha);
                    comando.Parameters.AddWithValue("@NumeroViaje", viaje.NumeroViaje);
                    comando.Parameters.AddWithValue("@Responsable", viaje.Responsable);
                    comando.Parameters.AddWithValue("@NumeroGuia", viaje.NumeroGuia);
                    comando.Parameters.AddWithValue("@PuntoPartida", viaje.PuntoPartida ?? "");
                    comando.Parameters.AddWithValue("@PuntoLlegada", viaje.PuntoLlegada ?? "");
                    comando.Parameters.AddWithValue("@VehiculoId", viaje.VehiculoId);
                    comando.Parameters.AddWithValue("@ConductorId", viaje.ConductorId);
                    comando.Parameters.AddWithValue("@Estado", viaje.Estado);
                    comando.Parameters.AddWithValue("@FechaModificacion", FechaOperacionalHelper.ObtenerFechaOperacionalActual());
                    comando.Parameters.AddWithValue("@UsuarioModificacion", Environment.UserName);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al actualizar viaje: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public void ReabrirViaje(int viajeId)
        {
            string consulta = @"  
        UPDATE VIAJES SET   
            Estado = 'Activo',  
            FechaModificacion = @FechaModificacion,  
            UsuarioModificacion = @UsuarioModificacion  
        WHERE ViajeId = @ViajeId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@ViajeId", viajeId);
                    comando.Parameters.AddWithValue("@FechaModificacion", FechaOperacionalHelper.ObtenerFechaOperacionalActual());
                    comando.Parameters.AddWithValue("@UsuarioModificacion", Environment.UserName);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al reabrir viaje: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public Vehiculo ObtenerVehiculoPorId(int vehiculoId)
        {
            Vehiculo vehiculo = null;
            string consulta = @"  
                SELECT v.VehiculoId, v.Placa, v.EmpresaId  
                FROM VEHICULOS v  
                WHERE v.VehiculoId = @VehiculoId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@VehiculoId", vehiculoId);

                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();

                        if (lector.Read())
                        {
                            vehiculo = new Vehiculo
                            {
                                VehiculoId = (int)lector["VehiculoId"],
                                Placa = lector["Placa"].ToString(),
                                EmpresaId = (int)lector["EmpresaId"]
                            };
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener vehículo: {ex.Message}");
                    }
                }
            }

            return vehiculo;
        }

        public void GuardarPalletViaje(InformacionPallet pallet, int viajeId)
        {
            string consulta = @"    
        INSERT INTO PALLETS_VIAJE (NumeroPallet, Variedad, Calibre, Embalaje, NumeroDeCajas,     
                                   ViajeId, PesoUnitario, PesoTotal, VariedadOriginal,     
                                   CalibreOriginal, EmbalajeOriginal, NumeroDeCajasOriginal,     
                                   FechaEscaneo, Modificado, FechaModificacion,  
                                   SegundaVariedad, CajasSegundaVariedad)    
        VALUES (@NumeroPallet, @Variedad, @Calibre, @Embalaje, @NumeroDeCajas,     
                @ViajeId, @PesoUnitario, @PesoTotal, @VariedadOriginal,     
                @CalibreOriginal, @EmbalajeOriginal, @NumeroDeCajasOriginal,     
                @FechaEscaneo, @Modificado, @FechaModificacion,  
                @SegundaVariedad, @CajasSegundaVariedad)";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    // Parámetros existentes  
                    comando.Parameters.AddWithValue("@NumeroPallet", pallet.NumeroPallet);
                    comando.Parameters.AddWithValue("@Variedad", pallet.Variedad ?? "");
                    comando.Parameters.AddWithValue("@Calibre", pallet.Calibre ?? "");
                    comando.Parameters.AddWithValue("@Embalaje", pallet.Embalaje ?? "");
                    comando.Parameters.AddWithValue("@NumeroDeCajas", pallet.NumeroDeCajas);
                    comando.Parameters.AddWithValue("@ViajeId", viajeId);
                    comando.Parameters.AddWithValue("@PesoUnitario", pallet.PesoUnitario);
                    comando.Parameters.AddWithValue("@PesoTotal", pallet.PesoTotal);
                    comando.Parameters.AddWithValue("@VariedadOriginal", pallet.VariedadOriginal ?? "");
                    comando.Parameters.AddWithValue("@CalibreOriginal", pallet.CalibreOriginal ?? "");
                    comando.Parameters.AddWithValue("@EmbalajeOriginal", pallet.EmbalajeOriginal ?? "");
                    comando.Parameters.AddWithValue("@NumeroDeCajasOriginal", pallet.NumeroDeCajasOriginal);
                    comando.Parameters.AddWithValue("@FechaEscaneo", FechaOperacionalHelper.ObtenerFechaOperacionalActual());
                    comando.Parameters.AddWithValue("@Modificado", pallet.Modificado);
                    comando.Parameters.AddWithValue("@FechaModificacion", pallet.FechaModificacion ?? FechaOperacionalHelper.ObtenerFechaOperacionalActual());

                    // NUEVOS PARÁMETROS PARA BICOLOR 
                    comando.Parameters.AddWithValue("@SegundaVariedad", pallet.SegundaVariedad ?? (object)DBNull.Value);
                    comando.Parameters.AddWithValue("@CajasSegundaVariedad", pallet.CajasSegundaVariedad);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al guardar pallet en viaje: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public PesoEmbalaje ObtenerPesoEmbalaje(string nombreEmbalaje)
        {
            PesoEmbalaje pesoEmbalaje = null;
            string consulta = @"    
        SELECT PesoEmbalajeId, NombreEmbalaje, PesoUnitario, TotalCajasFichaTecnica,  
               FechaCreacion, FechaModificacion, Activo    
        FROM PESOS_EMBALAJE     
        WHERE NombreEmbalaje = @NombreEmbalaje AND Activo = 1";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NombreEmbalaje", nombreEmbalaje);

                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();

                        if (lector.Read())
                        {
                            pesoEmbalaje = new PesoEmbalaje
                            {
                                PesoEmbalajeId = (int)lector["PesoEmbalajeId"],
                                NombreEmbalaje = lector["NombreEmbalaje"].ToString(),
                                PesoUnitario = (decimal)lector["PesoUnitario"],
                                TotalCajasFichaTecnica = lector["TotalCajasFichaTecnica"] as int?,
                                FechaCreacion = (DateTime)lector["FechaCreacion"],
                                FechaModificacion = lector["FechaModificacion"] as DateTime?,
                                Activo = (bool)lector["Activo"]
                            };
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener peso de embalaje: {ex.Message}");
                    }
                }
            }
            return pesoEmbalaje;
        }


        public List<Viaje> BuscarViajesConFiltros(string numeroGuia, int? empresaId, int? conductorId, DateTime? fechaDesde, DateTime? fechaHasta, string numeroPallet)
        {
            var viajes = new List<Viaje>();
            string consulta = @"    
        SELECT v.ViajeId, v.Fecha, v.NumeroViaje, v.Responsable, v.NumeroGuia,     
               v.PuntoPartida, v.PuntoLlegada, v.VehiculoId, v.ConductorId, v.Estado,    
               e.NombreEmpresa, c.NombreConductor, vh.Placa    
        FROM VIAJES v    
        INNER JOIN VEHICULOS vh ON v.VehiculoId = vh.VehiculoId    
        INNER JOIN EMPRESAS_TRANSPORTE e ON vh.EmpresaId = e.EmpresaId    
        INNER JOIN CONDUCTORES c ON v.ConductorId = c.ConductorId";

            // NUEVO: Agregar LEFT JOIN para búsqueda por pallet  
            if (!string.IsNullOrEmpty(numeroPallet))
            {
                consulta += " INNER JOIN PALLETS_VIAJE pv ON v.ViajeId = pv.ViajeId";
            }

            consulta += " WHERE 1=1";

            var parametros = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(numeroGuia))
            {
                consulta += " AND v.NumeroGuia LIKE @NumeroGuia";
                parametros.Add(new SqlParameter("@NumeroGuia", $"%{numeroGuia}%"));
            }

            // NUEVO: Filtro por número de pallet  
            if (!string.IsNullOrEmpty(numeroPallet))
            {
                consulta += " AND pv.NumeroPallet = @NumeroPallet";
                parametros.Add(new SqlParameter("@NumeroPallet", numeroPallet));
            }

            if (empresaId.HasValue)
            {
                consulta += " AND e.EmpresaId = @EmpresaId";
                parametros.Add(new SqlParameter("@EmpresaId", empresaId.Value));
            }

            if (conductorId.HasValue)
            {
                consulta += " AND c.ConductorId = @ConductorId";
                parametros.Add(new SqlParameter("@ConductorId", conductorId.Value));
            }

            if (fechaDesde.HasValue)
            {
                consulta += " AND v.Fecha >= @FechaDesde";
                parametros.Add(new SqlParameter("@FechaDesde", fechaDesde.Value.Date));
            }

            if (fechaHasta.HasValue)
            {
                consulta += " AND v.Fecha <= @FechaHasta";
                parametros.Add(new SqlParameter("@FechaHasta", fechaHasta.Value.Date));
            }

            // NUEVO: Agregar GROUP BY cuando se busca por pallet para evitar duplicados  
            if (!string.IsNullOrEmpty(numeroPallet))
            {
                consulta += @" GROUP BY v.ViajeId, v.Fecha, v.NumeroViaje, v.Responsable, v.NumeroGuia,     
                              v.PuntoPartida, v.PuntoLlegada, v.VehiculoId, v.ConductorId, v.Estado,    
                              e.NombreEmpresa, c.NombreConductor, vh.Placa";
            }

            consulta += " ORDER BY v.Fecha DESC, v.NumeroViaje DESC";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddRange(parametros.ToArray());

                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();
                        while (lector.Read())
                        {
                            viajes.Add(new Viaje
                            {
                                ViajeId = (int)lector["ViajeId"],
                                Fecha = (DateTime)lector["Fecha"],
                                NumeroViaje = (int)lector["NumeroViaje"],
                                Responsable = lector["Responsable"].ToString(),
                                NumeroGuia = lector["NumeroGuia"].ToString(),
                                PuntoPartida = lector["PuntoPartida"].ToString(),
                                PuntoLlegada = lector["PuntoLlegada"].ToString(),
                                VehiculoId = (int)lector["VehiculoId"],
                                ConductorId = (int)lector["ConductorId"],
                                Estado = lector["Estado"].ToString(),
                                NombreEmpresa = lector["NombreEmpresa"].ToString(),
                                NombreConductor = lector["NombreConductor"].ToString(),
                                PlacaVehiculo = lector["Placa"].ToString()
                            });
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al buscar viajes: {ex.Message}");
                    }
                }
            }

            return viajes;
        }

        public List<InformacionPallet> ObtenerPalletsDeViaje(int viajeId)
        {
            var pallets = new List<InformacionPallet>();
            string consulta = @"    
        SELECT NumeroPallet, Variedad, Calibre, Embalaje, NumeroDeCajas,     
               PesoUnitario, PesoTotal, VariedadOriginal, CalibreOriginal,     
               EmbalajeOriginal, NumeroDeCajasOriginal, FechaEscaneo,     
               Modificado, FechaModificacion, SegundaVariedad, CajasSegundaVariedad    
        FROM PALLETS_VIAJE     
        WHERE ViajeId = @ViajeId    
        ORDER BY FechaEscaneo";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@ViajeId", viajeId);

                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();
                        while (lector.Read())
                        {
                            var pallet = new InformacionPallet
                            {
                                NumeroPallet = lector["NumeroPallet"].ToString(),
                                Variedad = lector["Variedad"].ToString(),
                                Calibre = lector["Calibre"].ToString(),
                                Embalaje = lector["Embalaje"].ToString(),
                                NumeroDeCajas = (int)lector["NumeroDeCajas"],
                                PesoUnitario = (decimal)lector["PesoUnitario"],
                                PesoTotal = (decimal)lector["PesoTotal"],
                                VariedadOriginal = lector["VariedadOriginal"].ToString(),
                                CalibreOriginal = lector["CalibreOriginal"].ToString(),
                                EmbalajeOriginal = lector["EmbalajeOriginal"].ToString(),
                                NumeroDeCajasOriginal = (int)lector["NumeroDeCajasOriginal"],
                                Modificado = (bool)lector["Modificado"],
                                FechaModificacion = lector["FechaModificacion"] as DateTime?,

                                // NUEVOS CAMPOS BICOLOR  
                                SegundaVariedad = lector["SegundaVariedad"] as string,
                                CajasSegundaVariedad = lector["CajasSegundaVariedad"] as int? ?? 0
                            };

                            // Detectar si es bicolor 
                            if (_accesoDatosEmbalajeBicolor.EsEmbalajeBicolor(pallet.Embalaje) && !string.IsNullOrEmpty(pallet.SegundaVariedad))
                            {
                                pallet.EsBicolor = true;
                            }

                            pallets.Add(pallet);
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener pallets del viaje: {ex.Message}");
                    }
                }
            }

            return pallets;
        }


        public List<ReporteGeneralPallet> ObtenerPalletsEnviadosPorFechas(DateTime fechaDesde, DateTime fechaHasta, int? empresaId = null, int? conductorId = null)
        {
            var pallets = new List<ReporteGeneralPallet>();
            string consulta = @"      
    SELECT pv.PalletId, pv.NumeroPallet, pv.Variedad, pv.Calibre, pv.Embalaje,       
           pv.NumeroDeCajas, pv.PesoUnitario, pv.PesoTotal, pv.FechaEscaneo,       
           pv.Modificado, pv.EstadoEnvio, pv.FechaEnvio, pv.UsuarioEnvio,  
           pv.SegundaVariedad, pv.CajasSegundaVariedad,      
           v.ViajeId, v.Fecha as FechaViaje, v.NumeroViaje, v.NumeroGuia,       
           v.Responsable, v.PuntoPartida, v.PuntoLlegada, v.Estado as EstadoViaje,      
           v.FechaCreacion as FechaCreacionViaje, v.FechaModificacion as FechaModificacionViaje,      
           v.UsuarioCreacion as UsuarioCreacionViaje, v.UsuarioModificacion as UsuarioModificacionViaje,      
           e.NombreEmpresa, e.RUC as RUCEmpresa, c.NombreConductor, vh.Placa as PlacaVehiculo      
    FROM PALLETS_VIAJE pv      
    INNER JOIN VIAJES v ON pv.ViajeId = v.ViajeId      
    INNER JOIN VEHICULOS vh ON v.VehiculoId = vh.VehiculoId      
    INNER JOIN EMPRESAS_TRANSPORTE e ON vh.EmpresaId = e.EmpresaId      
    INNER JOIN CONDUCTORES c ON v.ConductorId = c.ConductorId      
    WHERE pv.EstadoEnvio = 'Enviado'    
    AND pv.FechaEnvio >= @FechaDesde      
    AND pv.FechaEnvio <= @FechaHasta";

            var parametros = new List<SqlParameter>
    {
        new SqlParameter("@FechaDesde", fechaDesde.Date),
        new SqlParameter("@FechaHasta", fechaHasta.Date.AddDays(1).AddSeconds(-1))
    };

            if (empresaId.HasValue)
            {
                consulta += " AND e.EmpresaId = @EmpresaId";
                parametros.Add(new SqlParameter("@EmpresaId", empresaId.Value));
            }

            if (conductorId.HasValue)
            {
                consulta += " AND c.ConductorId = @ConductorId";
                parametros.Add(new SqlParameter("@ConductorId", conductorId.Value));
            }

            consulta += " ORDER BY pv.FechaEnvio DESC, v.NumeroViaje, pv.NumeroPallet";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddRange(parametros.ToArray());

                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();
                        while (lector.Read())
                        {
                            var pallet = new ReporteGeneralPallet
                            {
                                // Información del Pallet    
                                PalletId = (int)lector["PalletId"],
                                NumeroPallet = lector["NumeroPallet"].ToString(),
                                Variedad = lector["Variedad"].ToString(),
                                Calibre = lector["Calibre"].ToString(),
                                Embalaje = lector["Embalaje"].ToString(),
                                NumeroDeCajas = (int)lector["NumeroDeCajas"],
                                PesoUnitario = (decimal)lector["PesoUnitario"],
                                PesoTotal = (decimal)lector["PesoTotal"],
                                FechaEscaneo = (DateTime)lector["FechaEscaneo"],
                                Modificado = (bool)lector["Modificado"],

                                // NUEVOS CAMPOS BICOLOR 
                                SegundaVariedad = lector["SegundaVariedad"] as string,
                                CajasSegundaVariedad = lector["CajasSegundaVariedad"] as int? ?? 0,

                                // Información de Envío    
                                EstadoEnvio = lector["EstadoEnvio"].ToString(),
                                FechaEnvio = lector["FechaEnvio"] as DateTime?,
                                UsuarioEnvio = lector["UsuarioEnvio"].ToString(),

                                // Información del Viaje    
                                ViajeId = (int)lector["ViajeId"],
                                FechaViaje = (DateTime)lector["FechaViaje"],
                                NumeroViaje = (int)lector["NumeroViaje"],
                                NumeroGuia = lector["NumeroGuia"].ToString(),
                                Responsable = lector["Responsable"].ToString(),
                                PuntoPartida = lector["PuntoPartida"].ToString(),
                                PuntoLlegada = lector["PuntoLlegada"].ToString(),
                                EstadoViaje = lector["EstadoViaje"].ToString(),
                                FechaCreacionViaje = (DateTime)lector["FechaCreacionViaje"],
                                FechaModificacionViaje = lector["FechaModificacionViaje"] as DateTime?,
                                UsuarioCreacionViaje = lector["UsuarioCreacionViaje"].ToString(),
                                UsuarioModificacionViaje = lector["UsuarioModificacionViaje"].ToString(),

                                // Información de Transporte    
                                NombreEmpresa = lector["NombreEmpresa"].ToString(),
                                RUCEmpresa = lector["RUCEmpresa"].ToString(),
                                NombreConductor = lector["NombreConductor"].ToString(),
                                PlacaVehiculo = lector["PlacaVehiculo"].ToString()
                            };

                            // DETECTAR SI ES BICOLOR
                            if (_accesoDatosEmbalajeBicolor.EsEmbalajeBicolor(pallet.Embalaje) && !string.IsNullOrEmpty(pallet.SegundaVariedad))
                            {
                                pallet.EsBicolor = true;
                            }


                            pallets.Add(pallet);
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener pallets por fechas: {ex.Message}");
                        throw;
                    }
                }
            }

            return pallets;
        }

        public List<PesoEmbalaje> ObtenerTodosPesosEmbalaje()
        {
            var pesos = new List<PesoEmbalaje>();
            string consulta = @"    
        SELECT PesoEmbalajeId, NombreEmbalaje, PesoUnitario, TotalCajasFichaTecnica,  
               FechaCreacion, FechaModificacion, Activo    
        FROM PESOS_EMBALAJE     
        ORDER BY NombreEmbalaje";

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
                            pesos.Add(new PesoEmbalaje
                            {
                                PesoEmbalajeId = (int)lector["PesoEmbalajeId"],
                                NombreEmbalaje = lector["NombreEmbalaje"].ToString(),
                                PesoUnitario = (decimal)lector["PesoUnitario"],
                                TotalCajasFichaTecnica = lector["TotalCajasFichaTecnica"] as int?,
                                FechaCreacion = (DateTime)lector["FechaCreacion"],
                                FechaModificacion = lector["FechaModificacion"] as DateTime?,
                                Activo = (bool)lector["Activo"]
                            });
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener pesos de embalaje: {ex.Message}");
                    }
                }
            }
            return pesos;
        }

        public void GuardarPesoEmbalaje(PesoEmbalaje nuevoPeso)
        {
            string consulta = @"    
        INSERT INTO PESOS_EMBALAJE (NombreEmbalaje, PesoUnitario, TotalCajasFichaTecnica, FechaCreacion, Activo)    
        VALUES (@NombreEmbalaje, @PesoUnitario, @TotalCajasFichaTecnica, @FechaCreacion, @Activo)";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NombreEmbalaje", nuevoPeso.NombreEmbalaje);
                    comando.Parameters.AddWithValue("@PesoUnitario", nuevoPeso.PesoUnitario);
                    comando.Parameters.AddWithValue("@TotalCajasFichaTecnica",
                        nuevoPeso.TotalCajasFichaTecnica.HasValue ? (object)nuevoPeso.TotalCajasFichaTecnica.Value : DBNull.Value);
                    comando.Parameters.AddWithValue("@FechaCreacion", FechaOperacionalHelper.ObtenerFechaOperacionalActual());
                    comando.Parameters.AddWithValue("@Activo", true);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al guardar peso de embalaje: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public void ActualizarPesoEmbalaje(PesoEmbalaje peso)
        {
            string consulta = @"    
        UPDATE PESOS_EMBALAJE SET     
            NombreEmbalaje = @NombreEmbalaje,    
            PesoUnitario = @PesoUnitario,    
            TotalCajasFichaTecnica = @TotalCajasFichaTecnica,  
            FechaModificacion = @FechaModificacion,    
            Activo = @Activo    
        WHERE PesoEmbalajeId = @PesoEmbalajeId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@PesoEmbalajeId", peso.PesoEmbalajeId);
                    comando.Parameters.AddWithValue("@NombreEmbalaje", peso.NombreEmbalaje);
                    comando.Parameters.AddWithValue("@PesoUnitario", peso.PesoUnitario);
                    comando.Parameters.AddWithValue("@TotalCajasFichaTecnica",
                        peso.TotalCajasFichaTecnica.HasValue ? (object)peso.TotalCajasFichaTecnica.Value : DBNull.Value);
                    comando.Parameters.AddWithValue("@FechaModificacion", FechaOperacionalHelper.ObtenerFechaOperacionalActual());
                    comando.Parameters.AddWithValue("@Activo", peso.Activo);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al actualizar peso de embalaje: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        // MÉTODOS EXISTENTES (mantener todos los que ya tienes)  
        public List<EmpresaTransporte> ObtenerEmpresas()
        {
            var empresas = new List<EmpresaTransporte>();
            string consulta = "SELECT EmpresaId, NombreEmpresa, RUC FROM EMPRESAS_TRANSPORTE";

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
                            empresas.Add(new EmpresaTransporte
                            {
                                EmpresaId = lector.GetInt32(lector.GetOrdinal("EmpresaId")),
                                NombreEmpresa = lector.GetString(lector.GetOrdinal("NombreEmpresa")),
                                RUC = lector.GetString(lector.GetOrdinal("RUC"))
                            });
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener empresas: {ex.Message}");
                    }
                }
            }
            return empresas;
        }

        public List<Vehiculo> ObtenerVehiculosPorEmpresa(int empresaId)
        {
            var vehiculos = new List<Vehiculo>();
            string consulta = @"  
        SELECT v.VehiculoId, v.Placa, v.EmpresaId, e.NombreEmpresa, e.RUC  
        FROM VEHICULOS v   
        INNER JOIN EMPRESAS_TRANSPORTE e ON v.EmpresaId = e.EmpresaId   
        WHERE v.EmpresaId = @EmpresaId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@EmpresaId", empresaId);
                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();
                        while (lector.Read())
                        {
                            vehiculos.Add(new Vehiculo
                            {
                                VehiculoId = lector.GetInt32(lector.GetOrdinal("VehiculoId")),
                                Placa = lector.GetString(lector.GetOrdinal("Placa")),
                                EmpresaId = lector.GetInt32(lector.GetOrdinal("EmpresaId")),
                                NombreEmpresa = lector.GetString(lector.GetOrdinal("NombreEmpresa"))
                            });
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener vehículos: {ex.Message}");
                    }
                }
            }
            return vehiculos;
        }

        public List<Conductor> ObtenerConductoresPorEmpresa(int empresaId)
        {
            var conductores = new List<Conductor>();
            string consulta = @"  
        SELECT c.ConductorId, c.NombreConductor, c.EmpresaId, e.NombreEmpresa, e.RUC  
        FROM CONDUCTORES c   
        INNER JOIN EMPRESAS_TRANSPORTE e ON c.EmpresaId = e.EmpresaId   
        WHERE c.EmpresaId = @EmpresaId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@EmpresaId", empresaId);
                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();
                        while (lector.Read())
                        {
                            conductores.Add(new Conductor
                            {
                                ConductorId = lector.GetInt32(lector.GetOrdinal("ConductorId")),
                                NombreConductor = lector.GetString(lector.GetOrdinal("NombreConductor")),
                                EmpresaId = lector.GetInt32(lector.GetOrdinal("EmpresaId")),
                                NombreEmpresa = lector.GetString(lector.GetOrdinal("NombreEmpresa"))
                            });
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener conductores: {ex.Message}");
                    }
                }
            }
            return conductores;
        }
        public List<Conductor> ObtenerTodosConductores()
        {
            var conductores = new List<Conductor>();
            string consulta = @"  
        SELECT c.ConductorId, c.NombreConductor, c.EmpresaId, e.NombreEmpresa, e.RUC  
        FROM CONDUCTORES c   
        INNER JOIN EMPRESAS_TRANSPORTE e ON c.EmpresaId = e.EmpresaId   
        ORDER BY e.NombreEmpresa, c.NombreConductor";

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
                            conductores.Add(new Conductor
                            {
                                ConductorId = lector.GetInt32(lector.GetOrdinal("ConductorId")),
                                NombreConductor = lector.GetString(lector.GetOrdinal("NombreConductor")),
                                EmpresaId = lector.GetInt32(lector.GetOrdinal("EmpresaId")),
                                NombreEmpresa = lector.GetString(lector.GetOrdinal("NombreEmpresa"))
                            });
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener todos los conductores: {ex.Message}");
                    }
                }
            }
            return conductores;
        }

        public List<Vehiculo> ObtenerTodosVehiculos()
        {
            var vehiculos = new List<Vehiculo>();
            string consulta = @"  
        SELECT v.VehiculoId, v.Placa, v.EmpresaId, e.NombreEmpresa, e.RUC  
        FROM VEHICULOS v   
        INNER JOIN EMPRESAS_TRANSPORTE e ON v.EmpresaId = e.EmpresaId   
        ORDER BY e.NombreEmpresa, v.Placa";

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
                            vehiculos.Add(new Vehiculo
                            {
                                VehiculoId = lector.GetInt32(lector.GetOrdinal("VehiculoId")),
                                Placa = lector.GetString(lector.GetOrdinal("Placa")),
                                EmpresaId = lector.GetInt32(lector.GetOrdinal("EmpresaId")),
                                NombreEmpresa = lector.GetString(lector.GetOrdinal("NombreEmpresa"))
                            });
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener todos los vehículos: {ex.Message}");
                    }
                }
            }
            return vehiculos;
        }
        public bool GuardarEmpresa(EmpresaTransporte nuevaEmpresa)
        {
            // Validar RUC duplicado antes de insertar  
            if (ExisteRUCEmpresa(nuevaEmpresa.RUC))
            {
                throw new InvalidOperationException($"Ya existe una empresa con el RUC: {nuevaEmpresa.RUC}");
            }

            string consulta = @"    
        INSERT INTO EMPRESAS_TRANSPORTE (NombreEmpresa, RUC)    
        VALUES (@NombreEmpresa, @RUC);";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NombreEmpresa", nuevaEmpresa.NombreEmpresa);
                    comando.Parameters.AddWithValue("@RUC", nuevaEmpresa.RUC);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al guardar la empresa: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public bool GuardarConductor(Conductor nuevoConductor)
        {
            // Validar nombre duplicado en la misma empresa antes de insertar  
            if (ExisteNombreConductorEnEmpresa(nuevoConductor.NombreConductor, nuevoConductor.EmpresaId))
            {
                return false; // Indicar que no se pudo guardar por duplicado  
            }

            string consulta = @"    
        INSERT INTO CONDUCTORES (NombreConductor, EmpresaId)    
        VALUES (@NombreConductor, @EmpresaId);";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NombreConductor", nuevoConductor.NombreConductor);
                    comando.Parameters.AddWithValue("@EmpresaId", nuevoConductor.EmpresaId);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                        return true; // Éxito  
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al guardar el conductor: {ex.Message}");
                        return false; // Error en la inserción  
                    }
                }
            }
        }
        public bool ExisteNombreConductorEnEmpresa(string nombreConductor, int empresaId, int? conductorIdExcluir = null)
        {
            string consulta = "SELECT COUNT(*) FROM CONDUCTORES WHERE NombreConductor = @NombreConductor AND EmpresaId = @EmpresaId";

            if (conductorIdExcluir.HasValue)
            {
                consulta += " AND ConductorId != @ConductorIdExcluir";
            }

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NombreConductor", nombreConductor);
                    comando.Parameters.AddWithValue("@EmpresaId", empresaId);

                    if (conductorIdExcluir.HasValue)
                    {
                        comando.Parameters.AddWithValue("@ConductorIdExcluir", conductorIdExcluir.Value);
                    }

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar conductor duplicado: {ex.Message}");
                        return true; // Por seguridad, asumir que existe  
                    }
                }
            }
        }

        public bool GuardarVehiculo(Vehiculo nuevoVehiculo)
        {
            // Validar placa duplicada antes de insertar  
            if (ExistePlacaVehiculo(nuevoVehiculo.Placa))
            {
                return false; // Indicar que no se pudo guardar por duplicado  
            }

            string consulta = @"    
        INSERT INTO VEHICULOS (Placa, EmpresaId)    
        VALUES (@Placa, @EmpresaId);";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@Placa", nuevoVehiculo.Placa);
                    comando.Parameters.AddWithValue("@EmpresaId", nuevoVehiculo.EmpresaId);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                        return true; // Éxito  
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al guardar el vehículo: {ex.Message}");
                        return false; // Error en la inserción  
                    }
                }
            }
        }

        public List<string> ObtenerTodosLosEmbalajes()
        {
            var embalajes = new List<string>();
            // Conectar a la base de datos Packing_SJP para obtener embalajes únicos  
            string cadenaConexionPacking = AppConfig.PackingSJPConnectionStringDynamic;
            string consulta = "SELECT DISTINCT DESCRIPCION FROM EMBALAJE WHERE DESCRIPCION IS NOT NULL AND DESCRIPCION != '' ORDER BY DESCRIPCION";
            using (SqlConnection conexion = new SqlConnection(cadenaConexionPacking))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();
                        while (lector.Read())
                        {
                            string descripcion = lector["DESCRIPCION"].ToString().Trim();
                            if (!string.IsNullOrEmpty(descripcion))
                            {
                                embalajes.Add(descripcion);
                            }
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener embalajes: {ex.Message}");
                    }
                }
            }

            return embalajes;
        }

        public List<Viaje> ObtenerViajesActivos()
        {
            var viajes = new List<Viaje>();
            string consulta = @"  
        SELECT v.ViajeId, v.Fecha, v.NumeroViaje, v.Responsable, v.NumeroGuia,   
               v.PuntoPartida, v.PuntoLlegada, v.VehiculoId, v.ConductorId, v.Estado,  
               e.NombreEmpresa, c.NombreConductor, vh.Placa  
        FROM VIAJES v  
        INNER JOIN VEHICULOS vh ON v.VehiculoId = vh.VehiculoId  
        INNER JOIN EMPRESAS_TRANSPORTE e ON vh.EmpresaId = e.EmpresaId  
        INNER JOIN CONDUCTORES c ON v.ConductorId = c.ConductorId  
        WHERE v.Estado = 'Activo'  
        ORDER BY v.Fecha DESC, v.NumeroViaje DESC";

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
                            viajes.Add(new Viaje
                            {
                                ViajeId = lector.GetInt32("ViajeId"),
                                Fecha = lector.GetDateTime("Fecha"),
                                NumeroViaje = lector.GetInt32("NumeroViaje"),
                                Responsable = lector.GetString("Responsable"),
                                NumeroGuia = lector.GetString("NumeroGuia"),
                                PuntoPartida = lector.GetString("PuntoPartida"),
                                PuntoLlegada = lector.GetString("PuntoLlegada"),
                                VehiculoId = lector.GetInt32("VehiculoId"),
                                ConductorId = lector.GetInt32("ConductorId"),
                                Estado = lector.GetString("Estado"),
                                NombreEmpresa = lector.GetString("NombreEmpresa"),
                                NombreConductor = lector.GetString("NombreConductor"),
                                PlacaVehiculo = lector.GetString("Placa")
                            });
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener viajes activos: {ex.Message}");
                    }
                }
            }
            return viajes;
        }

        public void ActualizarPalletViaje(InformacionPallet pallet, int viajeId)
        {
            string consulta = @"      
        UPDATE PALLETS_VIAJE SET       
            Variedad = @Variedad,      
            Calibre = @Calibre,      
            Embalaje = @Embalaje,      
            NumeroDeCajas = @NumeroDeCajas,      
            PesoUnitario = @PesoUnitario,      
            PesoTotal = @PesoTotal,      
            Modificado = @Modificado,      
            FechaModificacion = @FechaModificacion,  
            SegundaVariedad = @SegundaVariedad,  
            CajasSegundaVariedad = @CajasSegundaVariedad      
        WHERE NumeroPallet = @NumeroPallet AND ViajeId = @ViajeId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NumeroPallet", pallet.NumeroPallet);
                    comando.Parameters.AddWithValue("@Variedad", pallet.Variedad ?? "");
                    comando.Parameters.AddWithValue("@Calibre", pallet.Calibre ?? "");
                    comando.Parameters.AddWithValue("@Embalaje", pallet.Embalaje ?? "");
                    comando.Parameters.AddWithValue("@NumeroDeCajas", pallet.NumeroDeCajas);
                    comando.Parameters.AddWithValue("@PesoUnitario", pallet.PesoUnitario);
                    comando.Parameters.AddWithValue("@PesoTotal", pallet.PesoTotal);
                    comando.Parameters.AddWithValue("@Modificado", pallet.Modificado);
                    comando.Parameters.AddWithValue("@FechaModificacion", pallet.FechaModificacion ?? FechaOperacionalHelper.ObtenerFechaOperacionalActual());
                    comando.Parameters.AddWithValue("@ViajeId", viajeId);

                    // NUEVOS PARÁMETROS BICOLOR  
                    comando.Parameters.AddWithValue("@SegundaVariedad", pallet.SegundaVariedad ?? (object)DBNull.Value);
                    comando.Parameters.AddWithValue("@CajasSegundaVariedad", pallet.CajasSegundaVariedad);

                    try
                    {
                        conexion.Open();
                        comando.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al actualizar pallet en viaje: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public void MarcarPalletsComoEnviados(int viajeId, string usuarioEnvio)
        {
            var fechaOperacional = FechaOperacionalHelper.ObtenerFechaOperacionalActual();

            string consulta = @"  
        UPDATE PALLETS_VIAJE SET   
            EstadoEnvio = 'Enviado',  
            FechaEnvio = @FechaEnvio,  
            UsuarioEnvio = @UsuarioEnvio  
        WHERE ViajeId = @ViajeId   
        AND EstadoEnvio = 'Pendiente'";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@ViajeId", viajeId);
                    comando.Parameters.AddWithValue("@FechaEnvio", fechaOperacional);
                    comando.Parameters.AddWithValue("@UsuarioEnvio", usuarioEnvio);

                    try
                    {
                        conexion.Open();
                        int palletsActualizados = comando.ExecuteNonQuery();

                        var mensajeFecha = FechaOperacionalHelper.ObtenerMensajeDescriptivo(DateTime.Now);

                        System.Diagnostics.Debug.WriteLine(
                     $"Pallets marcados como enviados: {palletsActualizados} para viaje {viajeId}. {mensajeFecha}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al marcar pallets como enviados: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public List<string> ObtenerTodasLasVariedades()
        {
            var variedades = new List<string>();
            string cadenaConexionPacking = AppConfig.PackingSJPConnectionStringDynamic;

            string consulta = "SELECT DISTINCT Texto_Royalty FROM Royalty WHERE Texto_Royalty IS NOT NULL AND Texto_Royalty != '' ORDER BY Texto_Royalty";
            using (SqlConnection conexion = new SqlConnection(cadenaConexionPacking))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    try
                    {
                        conexion.Open();
                        SqlDataReader lector = comando.ExecuteReader();
                        while (lector.Read())
                        {
                            string variedad = lector["Texto_Royalty"].ToString().Trim();
                            if (!string.IsNullOrEmpty(variedad))
                            {
                                variedades.Add(variedad);
                            }
                        }
                        lector.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al obtener variedades: {ex.Message}");
                    }
                }
            }
            return variedades;
        }
        public void EliminarPalletViaje(string numeroPallet, int viajeId)
        {
            string consulta = @"    
        DELETE FROM PALLETS_VIAJE     
        WHERE NumeroPallet = @NumeroPallet AND ViajeId = @ViajeId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@NumeroPallet", numeroPallet);
                    comando.Parameters.AddWithValue("@ViajeId", viajeId);

                    try
                    {
                        conexion.Open();
                        int filasAfectadas = comando.ExecuteNonQuery();
                        if (filasAfectadas == 0)
                        {
                            throw new Exception("No se encontró el pallet para eliminar.");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al eliminar pallet del viaje: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public async Task<bool> MarcarViajeEnUsoAsync(string numeroGuia, string clienteId)
        {
            try
            {
                using var connection = new SqlConnection(AppConfig.GetConnectionString("DespachosSJP"));
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                try
                {
                    // Verificar si ya está en uso por otro cliente  
                    var checkCmd = new SqlCommand(@"  
                SELECT ClienteId FROM VIAJES_ESTADO   
                WHERE NumeroGuia = @NumeroGuia AND EnUso = 1", connection, transaction);
                    checkCmd.Parameters.AddWithValue("@NumeroGuia", numeroGuia);

                    var existingClient = await checkCmd.ExecuteScalarAsync() as string;
                    if (existingClient != null && existingClient != clienteId)
                    {
                        return false; // Ya está en uso por otro cliente  
                    }

                    // Marcar como en uso  
                    var cmd = new SqlCommand(@"  
                MERGE VIAJES_ESTADO AS target  
                USING (SELECT @NumeroGuia AS NumeroGuia) AS source  
                ON target.NumeroGuia = source.NumeroGuia  
                WHEN MATCHED THEN  
                    UPDATE SET EnUso = 1, ClienteId = @ClienteId, FechaUltimaActividad = GETDATE()  
                WHEN NOT MATCHED THEN  
                    INSERT (NumeroGuia, EnUso, ClienteId, FechaUltimaActividad)  
                    VALUES (@NumeroGuia, 1, @ClienteId, GETDATE());", connection, transaction);

                    cmd.Parameters.AddWithValue("@NumeroGuia", numeroGuia);
                    cmd.Parameters.AddWithValue("@ClienteId", clienteId);

                    await cmd.ExecuteNonQueryAsync();
                    transaction.Commit();

                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando viaje en uso: {NumeroGuia}", numeroGuia);
                return false;
            }
        }

        public async Task<bool> MarcarViajeLibreAsync(string numeroGuia, string clienteId)
        {
            try
            {
                using var connection = new SqlConnection(AppConfig.GetConnectionString("DespachosSJP"));
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"  
            UPDATE VIAJES_ESTADO   
            SET EnUso = 0, ClienteId = NULL, FechaUltimaActividad = GETDATE()  
            WHERE NumeroGuia = @NumeroGuia AND ClienteId = @ClienteId", connection);

                cmd.Parameters.AddWithValue("@NumeroGuia", numeroGuia);
                cmd.Parameters.AddWithValue("@ClienteId", clienteId);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liberando viaje: {NumeroGuia}", numeroGuia);
                return false;
            }
        }

        public async Task<Dictionary<string, bool>> ObtenerEstadosViajesAsync()
        {
            try
            {
                using var connection = new SqlConnection(AppConfig.GetConnectionString("DespachosSJP"));
                await connection.OpenAsync();

                // Limpiar viajes con timeout  
                var cleanupCmd = new SqlCommand(@"  
            UPDATE VIAJES_ESTADO   
            SET EnUso = 0, ClienteId = NULL   
            WHERE EnUso = 1 AND FechaUltimaActividad < DATEADD(MINUTE, -30, GETDATE())", connection);
                await cleanupCmd.ExecuteNonQueryAsync();

                // Obtener estados actuales  
                var cmd = new SqlCommand("SELECT NumeroGuia, EnUso FROM VIAJES_ESTADO", connection);
                var estados = new Dictionary<string, bool>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    estados[reader.GetString("NumeroGuia")] = reader.GetBoolean("EnUso");
                }

                return estados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estados de viajes");
                return new Dictionary<string, bool>();
            }
        }

        public async Task ActualizarHeartbeatAsync(string numeroGuia, string clienteId)
        {
            try
            {
                using var connection = new SqlConnection(AppConfig.GetConnectionString("DespachosSJP"));
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"  
            UPDATE VIAJES_ESTADO   
            SET FechaUltimaActividad = GETDATE()  
            WHERE NumeroGuia = @NumeroGuia AND ClienteId = @ClienteId", connection);

                cmd.Parameters.AddWithValue("@NumeroGuia", numeroGuia);
                cmd.Parameters.AddWithValue("@ClienteId", clienteId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando heartbeat: {NumeroGuia}", numeroGuia);
            }
        }
        public async Task<bool> VerificarViajeEnUsoAsync(string numeroGuia)
        {
            try
            {
                using var conexion = new SqlConnection(_cadenaConexion);
                using var comando = new SqlCommand(@"  
                SELECT CAST(CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END AS BIT)  
                FROM VIAJES_ESTADO   
                WHERE NumeroGuia = @NumeroGuia   
                AND EnUso = 1   
                AND FechaUltimaActividad > DATEADD(MINUTE, -30, GETDATE())", conexion);

                comando.Parameters.AddWithValue("@NumeroGuia", numeroGuia);
                comando.CommandTimeout = 5; // Timeout corto para consultas rápidas  

                await conexion.OpenAsync();
                var resultado = (bool)await comando.ExecuteScalarAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verificando viaje en uso: {numeroGuia}");
                return false;
            }
        }
        public async Task<List<string>> ObtenerViajesEnUsoAsync()
        {
            var viajesEnUso = new List<string>();

            try
            {
                using var conexion = new SqlConnection(_cadenaConexion); // CAMBIAR AQUÍ  
                await conexion.OpenAsync();

                var comando = new SqlCommand(@"    
                    SELECT NumeroGuia     
                    FROM VIAJES_ESTADO     
                    WHERE EnUso = 1     
                    AND FechaUltimaActividad > DATEADD(MINUTE, -30, GETDATE())", conexion);

                using var reader = await comando.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    viajesEnUso.Add(reader.GetString("NumeroGuia"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo viajes en uso: {ex.Message}");
            }

            return viajesEnUso;
        }
        public bool ActualizarEmpresa(EmpresaTransporte empresa)
        {
            string consulta = @"  
        UPDATE EMPRESAS_TRANSPORTE   
        SET NombreEmpresa = @NombreEmpresa, RUC = @RUC  
        WHERE EmpresaId = @EmpresaId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@EmpresaId", empresa.EmpresaId);
                    comando.Parameters.AddWithValue("@NombreEmpresa", empresa.NombreEmpresa);
                    comando.Parameters.AddWithValue("@RUC", empresa.RUC);

                    try
                    {
                        conexion.Open();
                        int filasAfectadas = comando.ExecuteNonQuery();
                        return filasAfectadas > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al actualizar empresa: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public bool ActualizarConductor(Conductor conductor)
        {
            string consulta = @"  
        UPDATE CONDUCTORES   
        SET NombreConductor = @NombreConductor, EmpresaId = @EmpresaId  
        WHERE ConductorId = @ConductorId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@ConductorId", conductor.ConductorId);
                    comando.Parameters.AddWithValue("@NombreConductor", conductor.NombreConductor);
                    comando.Parameters.AddWithValue("@EmpresaId", conductor.EmpresaId);

                    try
                    {
                        conexion.Open();
                        int filasAfectadas = comando.ExecuteNonQuery();
                        return filasAfectadas > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al actualizar conductor: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public bool ActualizarVehiculo(Vehiculo vehiculo)
        {
            string consulta = @"  
        UPDATE VEHICULOS   
        SET Placa = @Placa, EmpresaId = @EmpresaId  
        WHERE VehiculoId = @VehiculoId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@VehiculoId", vehiculo.VehiculoId);
                    comando.Parameters.AddWithValue("@Placa", vehiculo.Placa);
                    comando.Parameters.AddWithValue("@EmpresaId", vehiculo.EmpresaId);

                    try
                    {
                        conexion.Open();
                        int filasAfectadas = comando.ExecuteNonQuery();
                        return filasAfectadas > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al actualizar vehículo: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        public bool EliminarEmpresa(int empresaId)
        {
            // Verificar si tiene dependencias  
            if (TieneVehiculosAsociados(empresaId) || TieneConductoresAsociados(empresaId))
            {
                return false; // No se puede eliminar si tiene dependencias  
            }

            string consulta = "DELETE FROM EMPRESAS_TRANSPORTE WHERE EmpresaId = @EmpresaId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@EmpresaId", empresaId);

                    try
                    {
                        conexion.Open();
                        int filasAfectadas = comando.ExecuteNonQuery();
                        return filasAfectadas > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al eliminar empresa: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public bool EliminarConductor(int conductorId)
        {
            // Verificar si está siendo usado en viajes  
            if (TieneViajesAsociados(conductorId))
            {
                return false; // No se puede eliminar si tiene viajes asociados  
            }

            string consulta = "DELETE FROM CONDUCTORES WHERE ConductorId = @ConductorId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@ConductorId", conductorId);

                    try
                    {
                        conexion.Open();
                        int filasAfectadas = comando.ExecuteNonQuery();
                        return filasAfectadas > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al eliminar conductor: {ex.Message}");
                        return false;
                    }
                }
            }
        }

        public bool EliminarVehiculo(int vehiculoId)
        {
            // Verificar si está siendo usado en viajes  
            if (TieneViajesAsociadosVehiculo(vehiculoId))
            {
                return false; // No se puede eliminar si tiene viajes asociados  
            }

            string consulta = "DELETE FROM VEHICULOS WHERE VehiculoId = @VehiculoId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@VehiculoId", vehiculoId);

                    try
                    {
                        conexion.Open();
                        int filasAfectadas = comando.ExecuteNonQuery();
                        return filasAfectadas > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al eliminar vehículo: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        private bool TieneVehiculosAsociados(int empresaId)
        {
            string consulta = "SELECT COUNT(*) FROM VEHICULOS WHERE EmpresaId = @EmpresaId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@EmpresaId", empresaId);

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar vehículos: {ex.Message}");
                        return true; // En caso de error, asumir que tiene dependencias  
                    }
                }
            }
        }

        private bool TieneConductoresAsociados(int empresaId)
        {
            string consulta = "SELECT COUNT(*) FROM CONDUCTORES WHERE EmpresaId = @EmpresaId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@EmpresaId", empresaId);

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar conductores: {ex.Message}");
                        return true; // En caso de error, asumir que tiene dependencias  
                    }
                }
            }
        }

        private bool TieneViajesAsociados(int conductorId)
        {
            string consulta = "SELECT COUNT(*) FROM VIAJES WHERE ConductorId = @ConductorId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@ConductorId", conductorId);

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar viajes del conductor: {ex.Message}");
                        return true; // En caso de error, asumir que tiene dependencias  
                    }
                }
            }
        }

        private bool TieneViajesAsociadosVehiculo(int vehiculoId)
        {
            string consulta = "SELECT COUNT(*) FROM VIAJES WHERE VehiculoId = @VehiculoId";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@VehiculoId", vehiculoId);

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar viajes del vehículo: {ex.Message}");
                        return true; // En caso de error, asumir que tiene dependencias  
                    }
                }
            }
        }
        public bool ExisteRUCEmpresa(string ruc, int? empresaIdExcluir = null)
        {
            string consulta = "SELECT COUNT(*) FROM EMPRESAS_TRANSPORTE WHERE RUC = @RUC";

            if (empresaIdExcluir.HasValue)
            {
                consulta += " AND EmpresaId != @EmpresaIdExcluir";
            }

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@RUC", ruc);
                    if (empresaIdExcluir.HasValue)
                    {
                        comando.Parameters.AddWithValue("@EmpresaIdExcluir", empresaIdExcluir.Value);
                    }

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar RUC: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        public bool ExistePlacaVehiculo(string placa, int? vehiculoIdExcluir = null)
        {
            string consulta = "SELECT COUNT(*) FROM VEHICULOS WHERE Placa = @Placa";

            if (vehiculoIdExcluir.HasValue)
            {
                consulta += " AND VehiculoId != @VehiculoIdExcluir";
            }

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@Placa", placa.ToUpper());
                    if (vehiculoIdExcluir.HasValue)
                    {
                        comando.Parameters.AddWithValue("@VehiculoIdExcluir", vehiculoIdExcluir.Value);
                    }

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar placa: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        public bool EmpresaTieneDependencias(int empresaId)
        {
            string consulta = @"  
        SELECT   
            (SELECT COUNT(*) FROM CONDUCTORES WHERE EmpresaId = @EmpresaId) +  
            (SELECT COUNT(*) FROM VEHICULOS WHERE EmpresaId = @EmpresaId) AS TotalDependencias";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@EmpresaId", empresaId);

                    try
                    {
                        conexion.Open();
                        int totalDependencias = (int)comando.ExecuteScalar();
                        return totalDependencias > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar dependencias de empresa: {ex.Message}");
                        return true; // Por seguridad, asumir que tiene dependencias  
                    }
                }
            }
        }
        public bool ConductorTieneViajesActivos(int conductorId)
        {
            string consulta = @"  
        SELECT COUNT(*) FROM VIAJES   
        WHERE ConductorId = @ConductorId   
        AND Estado IN ('Activo', 'En Proceso')";

            using (SqlConnection conexion = new SqlConnection(_cadenaConexion))
            {
                using (SqlCommand comando = new SqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@ConductorId", conductorId);

                    try
                    {
                        conexion.Open();
                        int count = (int)comando.ExecuteScalar();
                        return count > 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar viajes del conductor: {ex.Message}");
                        return true; // Por seguridad, asumir que tiene viajes  
                    }
                }
            }
        }

    }
}