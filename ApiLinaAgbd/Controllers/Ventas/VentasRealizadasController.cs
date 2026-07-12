using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Ventas.VentasRealizadas;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{
	[ApiController]
	[Route("api/[controller]")]
	public class VentaRealizadaController : ControllerBase
	{
		private readonly Conexion _conexion;

		public VentaRealizadaController(Conexion conexion)
		{
			_conexion = conexion;
		}


		//========================================
		// LISTAR VENTAS REALIZADAS
		//========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{
			var lista = new List<VentaRealizadaSelectDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_VTA_SEL_VENTA_LISTAR",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;


				SqlDataReader dr = cmd.ExecuteReader();


				while (dr.Read())
				{
					lista.Add(new VentaRealizadaSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Cliente = dr["Cliente"].ToString(),
						Vendedor = dr["Vendedor"].ToString(),
						Fecha = Convert.ToDateTime(dr["fecha"]),
						CantidadProductos = Convert.ToInt32(dr["CantidadProductos"]),
						Total = Convert.ToDecimal(dr["Total"]),
						Estado = dr["estado"].ToString(),
						Igv = Convert.ToDecimal(dr["igv"])
					});
				}
			}


			return Ok(lista);
		}



		//========================================
		// OBTENER VENTA POR ID
		//========================================
		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{
			VentaRealizadaSelectDto venta = null;


			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_VTA_SEL_VENTA_OBTENER",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);


				SqlDataReader dr = cmd.ExecuteReader();


				if (dr.Read())
				{
					venta = new VentaRealizadaSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Cliente = dr["Cliente"].ToString(),
						Vendedor = dr["Vendedor"].ToString(),
						Fecha = Convert.ToDateTime(dr["fecha"]),
						Total = Convert.ToDecimal(dr["Total"]),
						Estado = dr["estado"].ToString(),
						Igv = Convert.ToDecimal(dr["igv"])
					};
				}
			}


			if (venta == null)
				return NotFound("Venta no encontrada");


			return Ok(venta);
		}



		//========================================
		// DETALLE DE VENTA
		//========================================
		[HttpGet("{id}/Detalle")]
		public IActionResult Detalle(int id)
		{
			var lista = new List<VentaRealizadaDetalleDto>();


			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_VTA_SEL_VENTA_DETALLE",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@IdVenta", id);


				SqlDataReader dr = cmd.ExecuteReader();


				while (dr.Read())
				{
					lista.Add(new VentaRealizadaDetalleDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Codigo = dr["codigo"].ToString(),
						Nombre = dr["nombre"].ToString(),
						Cantidad = Convert.ToInt32(dr["cantidad"]),
						PrecioUnitario = Convert.ToDecimal(dr["precioUnitario"]),
						SubTotal = Convert.ToDecimal(dr["SubTotal"])
					});
				}
			}


			return Ok(lista);
		}



		//========================================
		// PAGOS DE UNA VENTA
		//========================================
		[HttpGet("{id}/Pago")]
		public IActionResult Pago(int id)
		{
			var lista = new List<VentaRealizadaPagoDto>();


			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_VTA_SEL_VENTA_PAGO",
					con
				);

				cmd.CommandType = CommandType.StoredProcedure;


				cmd.Parameters.AddWithValue("@IdVenta", id);


				SqlDataReader dr = cmd.ExecuteReader();


				while (dr.Read())
				{
					lista.Add(new VentaRealizadaPagoDto
					{
						Id = Convert.ToInt32(dr["id"]),
						MetodoPago = dr["MetodoPago"].ToString(),
						Monto = Convert.ToDecimal(dr["monto"]),
						Fecha = Convert.ToDateTime(dr["fecha"]),
						CodigoOperacion = dr["codigo_operacion"].ToString()
					});
				}
			}


			return Ok(lista);
		}
	}
}