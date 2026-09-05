using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Inventario.Lote;
using ApiLinaAgbd.Models.Inventario.Lote_Stock;

namespace ApiLinaAgbd.Repositories.Inventario.Lote
{
	public class LoteRepository : ILoteRepository
	{
		private readonly Conexion _conexion;

		public LoteRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public (int IdLote, string CodigoLote) Insertar(LoteInsertDto modelo)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_LOT_INS_LOTE", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@IdProducto", modelo.id_producto);
				cmd.Parameters.AddWithValue("@IdDetalleCompra", modelo.id_detalle_compra);
				cmd.Parameters.AddWithValue("@FechaIngreso", modelo.fecha_ingreso);
				cmd.Parameters.AddWithValue("@FechaFabricacion",
					(object?)modelo.fecha_fabricacion ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@FechaVencimiento",
					(object?)modelo.fecha_vencimiento ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@CostoUnitario", modelo.costo_unitario);
				cmd.Parameters.AddWithValue("@Cantidad", modelo.cantidad);

				SqlDataReader dr = cmd.ExecuteReader();

				int idLote = 0;
				string codigoLote = string.Empty;

				if (dr.Read())
				{
					idLote = Convert.ToInt32(dr["IdLote"]);
					codigoLote = dr["CodigoLote"].ToString() ?? string.Empty;
				}

				return (idLote, codigoLote);
			}
		}

		public List<LoteSelectListarDto> Listar(LoteFiltroDto filtro)
		{
			List<LoteSelectListarDto> lista = new();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_LOT_SEL_LOTE_LISTAR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@CodigoLote", (object?)filtro.codigoLote ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@IdProducto", (object?)filtro.idProducto ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@IdProveedor", (object?)filtro.idProveedor ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@FechaIngresoDesde", (object?)filtro.fechaIngresoDesde ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@FechaIngresoHasta", (object?)filtro.fechaIngresoHasta ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@FechaVencimientoDesde", (object?)filtro.fechaVencimientoDesde ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@FechaVencimientoHasta", (object?)filtro.fechaVencimientoHasta ?? DBNull.Value);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new LoteSelectListarDto
					{
						id_lote = Convert.ToInt32(dr["id_lote"]),
						codigo_lote = dr["codigo_lote"].ToString() ?? string.Empty,
						id_producto = Convert.ToInt32(dr["id_producto"]),
						codigo_producto = dr["codigo_producto"].ToString() ?? string.Empty,
						producto = dr["producto"].ToString() ?? string.Empty,
						id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
						proveedor = dr["proveedor"].ToString() ?? string.Empty,
						fecha_ingreso = Convert.ToDateTime(dr["fecha_ingreso"]),
						fecha_fabricacion = dr["fecha_fabricacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["fecha_fabricacion"]),
						fecha_vencimiento = dr["fecha_vencimiento"] == DBNull.Value ? null : Convert.ToDateTime(dr["fecha_vencimiento"]),
						cantidad_ingresada = Convert.ToInt32(dr["cantidad_ingresada"]),
						costo_unitario = Convert.ToDecimal(dr["costo_unitario"]),
						valorCompra = Convert.ToDecimal(dr["ValorCompra"]),
						diasParaVencer = dr["DiasParaVencer"] == DBNull.Value ? null : Convert.ToInt32(dr["DiasParaVencer"]),
						estadoLote = dr["EstadoLote"].ToString() ?? string.Empty
					});
				}
			}

			return lista;
		}

		public LoteSelectDto? Obtener(int id)
		{
			LoteSelectDto? lote = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_LOT_SEL_LOTE_OBTENER", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@IdLote", id);

				SqlDataReader dr = cmd.ExecuteReader();

				if (dr.Read())
				{
					lote = new LoteSelectDto
					{
						id_lote = Convert.ToInt32(dr["id_lote"]),
						codigo_lote = dr["codigo_lote"].ToString() ?? string.Empty,

						id_producto = Convert.ToInt32(dr["id_producto"]),
						codigo_producto = dr["codigo_producto"].ToString() ?? string.Empty,
						producto = dr["producto"].ToString() ?? string.Empty,

						id_proveedor = Convert.ToInt32(dr["id_proveedor"]),
						proveedor = dr["proveedor"].ToString() ?? string.Empty,

						fecha_ingreso = Convert.ToDateTime(dr["fecha_ingreso"]),

						fecha_fabricacion = dr["fecha_fabricacion"] == DBNull.Value
							? null
							: Convert.ToDateTime(dr["fecha_fabricacion"]),

						fecha_vencimiento = dr["fecha_vencimiento"] == DBNull.Value
							? null
							: Convert.ToDateTime(dr["fecha_vencimiento"]),

						cantidad_ingresada = Convert.ToInt32(dr["cantidad_ingresada"]),

						costo_unitario = Convert.ToDecimal(dr["costo_unitario"]),

						valorCompra = Convert.ToDecimal(dr["ValorCompra"]),

						stockActual = dr["StockActual"] == DBNull.Value
							? null
							: Convert.ToInt32(dr["StockActual"]),

						diasParaVencer = dr["DiasParaVencer"] == DBNull.Value
							? null
							: Convert.ToInt32(dr["DiasParaVencer"]),

						estadoLote = dr["EstadoLote"].ToString() ?? string.Empty,

						movimientos = new List<LoteMovimientoDto>()
					};
				}

				if (lote != null && dr.NextResult())
				{
					while (dr.Read())
					{
						lote.movimientos.Add(new LoteMovimientoDto
						{
							id = Convert.ToInt32(dr["id"]),
							fecha = Convert.ToDateTime(dr["fecha"]),
							tipoMovimiento = dr["TipoMovimiento"].ToString() ?? string.Empty,
							cantidad = Convert.ToInt32(dr["Cantidad"]),
							motivo = dr["motivo"] == DBNull.Value
								? null
								: dr["motivo"].ToString()
						});
					}
				}
			}

			return lote;
		}

		public int InsertarMovimiento(MovimientoInsertDto modelo)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_MOV_INS_MOVIMIENTO", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@IdUsuario", modelo.id_usuario);
				cmd.Parameters.AddWithValue("@IdLote", modelo.id_lote);
				cmd.Parameters.AddWithValue("@IdProducto", modelo.id_producto);
				cmd.Parameters.AddWithValue("@Tipo", modelo.tipo);
				cmd.Parameters.AddWithValue("@Cantidad", modelo.cantidad);
				cmd.Parameters.AddWithValue("@Motivo",
					(object?)modelo.motivo ?? DBNull.Value);

				SqlDataReader dr = cmd.ExecuteReader();

				int idMovimiento = 0;

				if (dr.Read())
				{
					idMovimiento = Convert.ToInt32(dr["IdMovimiento"]);
				}

				return idMovimiento;
			}
		}

		public List<MovimientoSelectDto> ListarMovimientos(
			int? idProducto,
			int? tipo,
			DateTime? fechaDesde,
			DateTime? fechaHasta)
		{
			List<MovimientoSelectDto> lista = new();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_MOV_SEL_MOVIMIENTO_LISTAR",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdProducto",
					(object?)idProducto ?? DBNull.Value
				);

				cmd.Parameters.AddWithValue(
					"@Tipo",
					(object?)tipo ?? DBNull.Value
				);

				cmd.Parameters.AddWithValue(
					"@FechaDesde",
					(object?)fechaDesde ?? DBNull.Value
				);

				cmd.Parameters.AddWithValue(
					"@FechaHasta",
					(object?)fechaHasta ?? DBNull.Value
				);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new MovimientoSelectDto
					{
						idMovimiento = Convert.ToInt32(dr["id_movimiento"]),

						fecha = Convert.ToDateTime(dr["fecha"]),

						idTipoMovimiento = Convert.ToInt32(dr["id_tipo_movimiento"]),

						tipoMovimiento = dr["tipo_movimiento"].ToString() ?? "",

						idProducto = Convert.ToInt32(dr["id_producto"]),

						codigoProducto = dr["codigo_producto"].ToString() ?? "",

						producto = dr["producto"].ToString() ?? "",

						idLote = Convert.ToInt32(dr["id_lote"]),

						codigoLote = dr["codigo_lote"].ToString() ?? "",

						idUsuario = Convert.ToInt32(dr["id_usuario"]),

						usuario = dr["usuario"].ToString() ?? "",

						cantidad = Convert.ToInt32(dr["cantidad"]),

						motivo = dr["motivo"] == DBNull.Value
							? null
							: dr["motivo"].ToString(),

						stockActual = Convert.ToInt32(dr["stock_actual"])
					});
				}
			}

			return lista;
		}
	}
}
