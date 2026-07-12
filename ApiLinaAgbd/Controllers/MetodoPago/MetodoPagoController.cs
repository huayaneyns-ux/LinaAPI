using System.Data;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.MetodoPago;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.MetodoPago
{
	[ApiController]
	[Route("api/[controller]")]
	public class MetodoPagoController : ControllerBase
	{

		private readonly Conexion _conexion;


		public MetodoPagoController(Conexion conexion)
		{
			_conexion = conexion;
		}



		//=========================================
		// LISTAR METODOS DE PAGO
		//=========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{

			var lista = new List<MetodoPagoSelectDto>();


			using (SqlConnection con = _conexion.ObtenerConexion())
			{

				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_PRO_SEL_METODO_PAGO_LISTAR",
					con
				);


				cmd.CommandType = CommandType.StoredProcedure;



				SqlDataReader dr = cmd.ExecuteReader();



				while (dr.Read())
				{

					lista.Add(new MetodoPagoSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),

						Nombre = dr["nombre"].ToString(),

						Estado = Convert.ToBoolean(dr["estado"])
					});

				}

			}


			return Ok(lista);

		}

	}
}