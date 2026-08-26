using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Ventas.PedidosRecibidos;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{
	[ApiController]
	[Route("api/[controller]")]
	public class PedidosRecibidosController : ControllerBase
	{
		private readonly Conexion _conexion;

		public PedidosRecibidosController(Conexion conexion)
		{
			_conexion = conexion;
		}

		//=========================================
		// INSERTAR PEDIDO
		//=========================================
		[HttpPost("Insertar")]
		public IActionResult InsertarPedido(PedidoInsertDto modelo)
		{
			try
			{
				int idPedido = 0;

				//=========================================
				// INSERTAR CABECERA
				//=========================================
				using (SqlConnection con = _conexion.ObtenerConexion())
				{
					con.Open();

					SqlCommand cmd = new SqlCommand("USP_PED_INS_PEDIDO", con);
					cmd.CommandType = CommandType.StoredProcedure;

					cmd.Parameters.AddWithValue("@IdCliente", modelo.idCliente);
					cmd.Parameters.AddWithValue("@IdDireccion", modelo.idDireccion);
					cmd.Parameters.AddWithValue("@FechaPedido", modelo.fechaPedido);
					cmd.Parameters.AddWithValue("@FechaEntrega", (object?)modelo.fechaEntrega ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@TipoEntrega", modelo.tipoEntrega);
					cmd.Parameters.AddWithValue("@IGV", modelo.igv);

					cmd.Parameters.AddWithValue("@IdMetodoPago", modelo.idMetodoPago);
					cmd.Parameters.AddWithValue("@Monto", modelo.monto);
					cmd.Parameters.AddWithValue("@CodigoOperacion", (object?)modelo.codigoOperacion ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@RutaComprobante", (object?)modelo.rutaComprobante ?? DBNull.Value);

					SqlDataReader dr = cmd.ExecuteReader();

					if (dr.Read())
					{
						idPedido = Convert.ToInt32(dr["IdPedido"]);
					}

					dr.Close();
				}

				//=========================================
				// INSERTAR DETALLE
				//=========================================
				foreach (var item in modelo.detalle)
				{
					using (SqlConnection con = _conexion.ObtenerConexion())
					{
						con.Open();

						SqlCommand cmd = new SqlCommand("USP_PED_INS_DETALLE_PEDIDO", con);
						cmd.CommandType = CommandType.StoredProcedure;

						cmd.Parameters.AddWithValue("@IdPedido", idPedido);
						cmd.Parameters.AddWithValue("@IdProducto", item.idProducto);
						cmd.Parameters.AddWithValue("@Cantidad", item.cantidad);

						cmd.ExecuteNonQuery();
					}
				}

				return Ok(new
				{
					success = true,
					mensaje = "Pedido registrado correctamente.",
					idPedido
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					success = false,
					mensaje = ex.Message
				});
			}

		}
		//=========================================
		// CAMBIAR ESTADO PEDIDO
		//=========================================
		[HttpPut("CambiarEstado")]
		public IActionResult CambiarEstado(PedidoUpdateEstadoDto modelo)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PED_UPD_ESTADO_PEDIDO", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@IdPedido", modelo.id_pedido);
				cmd.Parameters.AddWithValue("@EstadoPedido", modelo.estado_pedido);

				cmd.ExecuteNonQuery();
			}

			return Ok(new
			{
				success = true,
				mensaje = "Estado del pedido actualizado correctamente."
			});
		}
		//=========================================
		// OBTENER PEDIDO POR ID
		//=========================================
		[HttpGet("{id}")]
		public IActionResult ObtenerPedido(int id)
		{
			PedidoSelectIdDto? pedido = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PED_SEL_PEDIDO_ID", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@IdPedido", id);

				SqlDataReader dr = cmd.ExecuteReader();

				//=========================================
				// CABECERA
				//=========================================
				if (dr.Read())
				{
					pedido = new PedidoSelectIdDto
					{
						id_pedido = Convert.ToInt32(dr["IdPedido"]),
						id_cliente = Convert.ToInt32(dr["id_cliente"]),
						cliente = dr["Cliente"].ToString() ?? string.Empty,
						telefono = dr["telefono"].ToString() ?? string.Empty,

						fecha_pedido = Convert.ToDateTime(dr["fecha_pedido"]),
						fecha_entrega = dr["fecha_entrega"] == DBNull.Value
							? null
							: Convert.ToDateTime(dr["fecha_entrega"]),

						tipo_entrega = dr["tipo_entrega"].ToString() ?? string.Empty,

						igv = Convert.ToDecimal(dr["igv"]),

						ruta_comprobante = dr["ruta_comprobante"] == DBNull.Value
							? null
							: dr["ruta_comprobante"].ToString(),

						estadoPedido = Convert.ToInt32(dr["EstadoPedido"]),
						estadoPedidoNombre = dr["EstadoPedidoNombre"].ToString() ?? string.Empty,

						id_pago = dr["IdPago"] == DBNull.Value
							? null
							: Convert.ToInt32(dr["IdPago"]),

						monto = dr["monto"] == DBNull.Value
							? null
							: Convert.ToDecimal(dr["monto"]),

						metodoPago = dr["MetodoPago"] == DBNull.Value
							? null
							: dr["MetodoPago"].ToString(),

						codigo_operacion = dr["codigo_operacion"] == DBNull.Value
							? null
							: dr["codigo_operacion"].ToString(),

						detalle = new List<PedidoDetalleDto>()
					};
				}

				//=========================================
				// DETALLE
				//=========================================
				if (pedido != null && dr.NextResult())
				{
					while (dr.Read())
					{
						pedido.detalle.Add(new PedidoDetalleDto
						{
							id_detalle_pedido = Convert.ToInt32(dr["IdDetallePedido"]),
							id_producto = Convert.ToInt32(dr["id_producto"]),
							producto = dr["Producto"].ToString() ?? string.Empty,
							codigo = dr["codigo"].ToString() ?? string.Empty,

							ruta_imagen = dr["ruta_imagen"] == DBNull.Value
								? null
								: dr["ruta_imagen"].ToString(),

							cantidad = Convert.ToInt32(dr["cantidad"]),
							precio_venta = Convert.ToDecimal(dr["precio_venta"])
						});
					}
				}

				dr.Close();
			}

			if (pedido == null)
				return NotFound();

			return Ok(pedido);
		}
		//=========================================
		// LISTAR PEDIDOS
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarPedidos()
		{
			var lista = new List<PedidoSelectDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_PED_SEL_PEDIDO_LISTAR",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;


				SqlDataReader dr = cmd.ExecuteReader();


				while (dr.Read())
				{
					lista.Add(new PedidoSelectDto
					{
						id_pedido = Convert.ToInt32(dr["IdPedido"]),

						fecha_pedido = Convert.ToDateTime(dr["fecha_pedido"]),

						fecha_entrega = dr["fecha_entrega"] == DBNull.Value
							? null
							: Convert.ToDateTime(dr["fecha_entrega"]),

						tipo_entrega = dr["tipo_entrega"].ToString(),

						igv = Convert.ToDecimal(dr["igv"]),

						ruta_comprobante = dr["ruta_comprobante"] == DBNull.Value
							? null
							: dr["ruta_comprobante"].ToString(),


						estadoPedido = Convert.ToInt32(dr["EstadoPedido"]),

						estadoPedidoNombre = dr["EstadoPedidoNombre"].ToString(),


						id_cliente = Convert.ToInt32(dr["id_cliente"]),

						cliente = dr["Cliente"].ToString(),

						telefono = dr["telefono"].ToString(),


						id_pago = dr["IdPago"] == DBNull.Value
							? null
							: Convert.ToInt32(dr["IdPago"]),


						monto = dr["monto"] == DBNull.Value
							? null
							: Convert.ToDecimal(dr["monto"]),


						codigo_operacion = dr["codigo_operacion"] == DBNull.Value
							? null
							: dr["codigo_operacion"].ToString(),


						metodoPago = dr["MetodoPago"] == DBNull.Value
							? null
							: dr["MetodoPago"].ToString()
					});
				}
			}


			return Ok(lista);
		}
	}
}