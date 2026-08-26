using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Ventas.Caja;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{ }
		[ApiController]
		[Route("api/[controller]")]
		public class CajaController : ControllerBase
		{

			private readonly Conexion _conexion;


			public CajaController(Conexion conexion)
			{
				_conexion = conexion;
			}



			//================================================
			// REGISTRAR VENTA COMPLETA
			//================================================

			[HttpPost("RegistrarVenta")]
			public IActionResult RegistrarVenta(
				[FromBody] CajaVentaInsertDto venta)
			{

				int idVenta = 0;


				using (SqlConnection con = _conexion.ObtenerConexion())
				{

					con.Open();


					SqlTransaction transaction = con.BeginTransaction();


					try
					{

						//====================================
						// 1. INSERTAR CABECERA
						//====================================

						SqlCommand cmdVenta =
							new SqlCommand(
								"USP_VTA_INS_VENTA",
								con,
								transaction);


						cmdVenta.CommandType =
							CommandType.StoredProcedure;


						cmdVenta.Parameters.AddWithValue(
							"@IdCliente",
							venta.IdCliente);


						cmdVenta.Parameters.AddWithValue(
							"@IdUsuario",
							venta.IdUsuario);


						cmdVenta.Parameters.AddWithValue(
							"@Fecha",
							DateTime.Now);


						cmdVenta.Parameters.AddWithValue(
							"@Estado",
							"Completada");


						cmdVenta.Parameters.AddWithValue(
							"@IGV",
							venta.Igv);



						idVenta = Convert.ToInt32(
							cmdVenta.ExecuteScalar()
						);




				//====================================
				// 2. INSERTAR DETALLE
				//====================================

				foreach (var item in venta.Detalle)
				{
					SqlCommand cmdDetalle = new SqlCommand(
						"USP_VTA_INS_DETALLE",
						con,
						transaction);

					cmdDetalle.CommandType = CommandType.StoredProcedure;

					cmdDetalle.Parameters.AddWithValue("@IdVenta", idVenta);
					cmdDetalle.Parameters.AddWithValue("@IdProducto", item.IdProducto);
					cmdDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
					cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", item.PrecioUnitario);


					List<(int IdDetalleVenta, int IdLote, int Cantidad)> lotes = new();


					using (SqlDataReader dr = cmdDetalle.ExecuteReader())
					{
						while (dr.Read())
						{
							lotes.Add((
								Convert.ToInt32(dr["IdDetalleVenta"]),
								Convert.ToInt32(dr["IdLote"]),
								Convert.ToInt32(dr["Cantidad"])
							));
						}
					}



					foreach (var lote in lotes)
					{

						//====================================
						// GUARDAR DETALLE VENTA LOTE
						//====================================

						SqlCommand cmdDetalleLote = new SqlCommand(
							"USP_VTA_INS_DETALLE_LOTE",
							con,
							transaction);

						cmdDetalleLote.CommandType = CommandType.StoredProcedure;

						cmdDetalleLote.Parameters.AddWithValue("@IdDetalleVenta", lote.IdDetalleVenta);
						cmdDetalleLote.Parameters.AddWithValue("@IdLote", lote.IdLote);
						cmdDetalleLote.Parameters.AddWithValue("@Cantidad", lote.Cantidad);

						cmdDetalleLote.ExecuteNonQuery();



						//====================================
						// REGISTRAR MOVIMIENTO INVENTARIO
						//====================================

						SqlCommand cmdMovimiento = new SqlCommand(
							"USP_MOV_INS_MOVIMIENTO",
							con,
							transaction);

						cmdMovimiento.CommandType = CommandType.StoredProcedure;

						cmdMovimiento.Parameters.AddWithValue("@IdUsuario", venta.IdUsuario);
						cmdMovimiento.Parameters.AddWithValue("@IdLote", lote.IdLote);
						cmdMovimiento.Parameters.AddWithValue("@IdProducto", item.IdProducto);
						cmdMovimiento.Parameters.AddWithValue("@Tipo", 2); // Venta
						cmdMovimiento.Parameters.AddWithValue("@Cantidad", lote.Cantidad);
						cmdMovimiento.Parameters.AddWithValue("@Motivo", $"Venta N° {idVenta}");

						cmdMovimiento.ExecuteNonQuery();

					}
				}




				//====================================
				// 3. INSERTAR PAGOS
				//====================================

				foreach (var pago in venta.Pagos)
						{

							SqlCommand cmdPago =
								new SqlCommand(
									"USP_VTA_INS_PAGO",
									con,
									transaction);


							cmdPago.CommandType =
								CommandType.StoredProcedure;



							cmdPago.Parameters.AddWithValue(
								"@IdVenta",
								idVenta);


							cmdPago.Parameters.AddWithValue(
								"@IdMetodoPago",
								pago.IdMetodoPago);


							cmdPago.Parameters.AddWithValue(
								"@Monto",
								pago.Monto);


							cmdPago.Parameters.AddWithValue(
								"@CodigoOperacion",
								pago.CodigoOperacion ?? "");



							cmdPago.ExecuteNonQuery();

						}




						transaction.Commit();


					}
					catch (Exception ex)
					{

						transaction.Rollback();


						return BadRequest(new
						{
							mensaje = "Error al registrar venta",
							error = ex.Message
						});

					}

				}

				return Ok(new CajaVentaResponseDto
				{
					IdVenta = idVenta,
					Mensaje = "Venta registrada correctamente"
				});

			}
			//================================================
			// BUSCAR CLIENTE POR DNI
			//================================================

			[HttpGet("Cliente/{dni}")]
			public IActionResult BuscarCliente(string dni)
			{

				CajaClienteDto cliente = null;


				using (SqlConnection con = _conexion.ObtenerConexion())
				{

					con.Open();


					SqlCommand cmd = new SqlCommand(
						"USP_USU_SEL_USUARIO_DNI",
						con
					);


					cmd.CommandType = CommandType.StoredProcedure;


					cmd.Parameters.AddWithValue(
						"@Dni",
						dni
					);


					SqlDataReader dr = cmd.ExecuteReader();


					if (dr.Read())
					{

						cliente = new CajaClienteDto
						{
							Id = Convert.ToInt32(dr["id"]),
							NombreApellido = dr["nombre_apellido"].ToString(),
							DNI = dr["dni"].ToString(),
							Telefono = dr["telefono"].ToString(),
							Correo = dr["correo"].ToString()
						};

					}

				}


				if (cliente == null)
					return NotFound("Cliente no encontrado");


				return Ok(cliente);

			}
			//================================================
			// CREAR CLIENTE
			//================================================

			[HttpPost("Cliente")]
			public IActionResult CrearCliente(
				[FromBody] CajaClienteInsertDto cliente)
			{


				int idUsuario = 0;


				using (SqlConnection con = _conexion.ObtenerConexion())
				{

					con.Open();


					SqlCommand cmd = new SqlCommand(
						"USP_USU_INS_CLIENTE",
						con
					);


					cmd.CommandType =
						CommandType.StoredProcedure;


					cmd.Parameters.AddWithValue(
						"@NombreApellido",
						cliente.NombreApellido
					);


					cmd.Parameters.AddWithValue(
						"@Dni",
						cliente.DNI
					);


					cmd.Parameters.AddWithValue(
						"@Telefono",
						cliente.Telefono ?? ""
					);


					cmd.Parameters.AddWithValue(
						"@Correo",
						cliente.Correo ?? ""
					);



					idUsuario = Convert.ToInt32(
						cmd.ExecuteScalar()
					);

				}



				return Ok(new
				{
					idCliente = idUsuario,
					mensaje = "Cliente registrado correctamente"
				});

			}
	[HttpPost("{id}/Pago")]
	public IActionResult RegistrarPago(
	int id,
	[FromBody] CajaPagoInsertDto pago)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_PRO_INS_PAGO",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;


				cmd.Parameters.AddWithValue("@id_venta", id);

				cmd.Parameters.AddWithValue(
					"@id_metodo_pago",
					pago.IdMetodoPago
				);

				cmd.Parameters.AddWithValue(
					"@monto",
					pago.Monto
				);

				cmd.Parameters.AddWithValue(
					"@fecha",
					pago.Fecha
				);

				cmd.Parameters.AddWithValue(
					"@codigo_operacion",
					(object?)pago.CodigoOperacion ?? DBNull.Value
				);


				cmd.ExecuteNonQuery();
			}


			return Ok("Pago registrado correctamente.");
		}
}
