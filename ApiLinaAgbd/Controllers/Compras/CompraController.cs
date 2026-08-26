using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Compras.Compra;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Compra
{

	[ApiController]
	[Route("api/[controller]")]
	public class CompraController : ControllerBase
	{

		private readonly Conexion _conexion;


		public CompraController(Conexion conexion)
		{
			_conexion = conexion;
		}



		[HttpPost("RegistrarCompleta")]
		public IActionResult RegistrarCompleta(CompraCompletaInsertDto modelo)
		{
			using SqlConnection con = _conexion.ObtenerConexion();

			con.Open();

			SqlTransaction trans = con.BeginTransaction();

			try
			{
				//-------------------------------------------------
				// CABECERA
				//-------------------------------------------------

				int idCompra;

				using (SqlCommand cmd = new SqlCommand("USP_COM_INS_COMPRA", con, trans))
				{
					cmd.CommandType = CommandType.StoredProcedure;

					cmd.Parameters.AddWithValue("@IdUsuario", modelo.id_usuario);
					cmd.Parameters.AddWithValue("@IdProveedor", modelo.id_proveedor);
					cmd.Parameters.AddWithValue("@FechaCompra", modelo.fecha_compra);
					cmd.Parameters.AddWithValue("@FechaRecepcion",
						(object?)modelo.fecha_recepcion ?? DBNull.Value);

					idCompra = Convert.ToInt32(cmd.ExecuteScalar());
				}

				//-------------------------------------------------
				// DETALLES
				//-------------------------------------------------

				foreach (var item in modelo.detalles)
				{
					int idDetalleCompra;

					using (SqlCommand cmd = new SqlCommand("USP_COM_INS_DETALLE", con, trans))
					{
						cmd.CommandType = CommandType.StoredProcedure;

						cmd.Parameters.AddWithValue("@IdCompra", idCompra);
						cmd.Parameters.AddWithValue("@IdProducto", item.id_producto);
						cmd.Parameters.AddWithValue("@Cantidad", item.cantidad);
						cmd.Parameters.AddWithValue("@CostoTotal", item.costo_total);

						idDetalleCompra = Convert.ToInt32(cmd.ExecuteScalar());
					}

					//---------------------------------------------
					// LOTE
					//---------------------------------------------

					int idLote = 0;

					using (SqlCommand cmd = new SqlCommand("USP_LOT_INS_LOTE", con, trans))
					{
						cmd.CommandType = CommandType.StoredProcedure;

						cmd.Parameters.AddWithValue("@IdDetalleCompra", idDetalleCompra);

						cmd.Parameters.AddWithValue(
							"@FechaFabricacion",
							(object?)item.fecha_fabricacion ?? DBNull.Value
						);

						cmd.Parameters.AddWithValue(
							"@FechaVencimiento",
							(object?)item.fecha_vencimiento ?? DBNull.Value
						);

						using SqlDataReader dr = cmd.ExecuteReader();

						if (dr.Read())
						{
							idLote = Convert.ToInt32(dr["IdLote"]);
						}
					}

					//---------------------------------------------
					// MOVIMIENTO
					//---------------------------------------------

					using (SqlCommand cmd = new SqlCommand("USP_MOV_INS_MOVIMIENTO", con, trans))
					{
						cmd.CommandType = CommandType.StoredProcedure;

						cmd.Parameters.AddWithValue("@IdUsuario", modelo.id_usuario);
						cmd.Parameters.AddWithValue("@IdLote", idLote);
						cmd.Parameters.AddWithValue("@IdProducto", item.id_producto);
						cmd.Parameters.AddWithValue("@Tipo", 1); // EntradaCompra
						cmd.Parameters.AddWithValue("@Cantidad", item.cantidad);
						cmd.Parameters.AddWithValue("@Motivo", "Compra");

						cmd.ExecuteNonQuery();
					}
				}

				trans.Commit();

				return Ok(new
				{
					success = true,
					mensaje = "Compra registrada correctamente.",
					idCompra
				});
			}
			catch (Exception ex)
			{
				trans.Rollback();

				return BadRequest(new
				{
					success = false,
					mensaje = ex.Message
				});
			}
		}



		//=========================================
		// DETALLE DE COMPRA
		//=========================================
		[HttpGet("{id}/Detalle")]
		public IActionResult Detalle(int id)
		{
			List<CompraDetalleSelectDto> lista = new();


			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				using (SqlCommand cmd = new SqlCommand(
					"USP_COM_SEL_COMPRA_DETALLE",
					con))
				{

					cmd.CommandType = CommandType.StoredProcedure;


					cmd.Parameters.AddWithValue(
						"@IdCompra",
						id
					);


					using (SqlDataReader dr = cmd.ExecuteReader())
					{

						while (dr.Read())
						{
							lista.Add(new CompraDetalleSelectDto
							{

								id_detalle_compra =
									Convert.ToInt32(
										dr["id_detalle_compra"]
									),


								id_producto =
									Convert.ToInt32(
										dr["id_producto"]
									),


								codigo_producto =
									dr["codigo_producto"]
									.ToString() ?? "",


								producto =
									dr["producto"]
									.ToString() ?? "",


								cantidad =
									Convert.ToInt32(
										dr["cantidad"]
									),


								costo_total =
									Convert.ToDecimal(
										dr["costo_total"]
									),


								costo_unitario =
									Convert.ToDecimal(
										dr["costo_unitario"]
									),


								id_lote =
									dr["id_lote"] == DBNull.Value
									? null
									: Convert.ToInt32(
										dr["id_lote"]
									),


								codigo_lote =
									dr["codigo_lote"] == DBNull.Value
									? null
									: dr["codigo_lote"]
									.ToString(),


								fecha_vencimiento =
									dr["fecha_vencimiento"] == DBNull.Value
									? null
									: Convert.ToDateTime(
										dr["fecha_vencimiento"]
									),


								stock_actual =
									dr["stock_actual"] == DBNull.Value
									? null
									: Convert.ToInt32(
										dr["stock_actual"]
									)

							});
						}
					}
				}
			}


			return Ok(lista);
		}


		//=========================================
		// LISTAR COMPRAS CABECERA
		//=========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{
			List<CompraListaDto> lista = new();


			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				using (SqlCommand cmd = new SqlCommand(
					"USP_COM_SEL_COMPRA_LISTAR",
					con))
				{

					cmd.CommandType = CommandType.StoredProcedure;


					using (SqlDataReader dr = cmd.ExecuteReader())
					{

						while (dr.Read())
						{
							lista.Add(new CompraListaDto
							{

								id_compra = Convert.ToInt32(
									dr["id_compra"]
								),


								id_usuario = Convert.ToInt32(
									dr["id_usuario"]
								),


								usuario = dr["usuario"]
									.ToString() ?? "",


								id_proveedor = Convert.ToInt32(
									dr["id_proveedor"]
								),


								proveedor = dr["proveedor"]
									.ToString() ?? "",


								fecha_compra = Convert.ToDateTime(
									dr["fecha_compra"]
								),


								fecha_recepcion =
									dr["fecha_recepcion"] == DBNull.Value
									? null
									: Convert.ToDateTime(
										dr["fecha_recepcion"]
									  ),


								total_compra =
									dr["total_compra"] == DBNull.Value
									? 0
									: Convert.ToDecimal(
										dr["total_compra"]
									  ),


								estado = Convert.ToBoolean(
									dr["estado"]
								)

							});
						}
					}
				}
			}


			return Ok(lista);
		}
	}
}