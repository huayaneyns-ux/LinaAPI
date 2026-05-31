using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MarcaController : ControllerBase
	{
		private readonly Conexion _conexion;

		public MarcaController(Conexion conexion)
		{
			_conexion = conexion;
		}

		[HttpGet]
		public IActionResult ObtenerMarcas()
		{
			var lista = new List<object>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("SELECT * FROM Marca", con);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new
					{
						id = dr["id"],
						nombre = dr["nombre"],
						estado = dr["estado"],
						url = dr["url_imagen"]
					});
				}
			}

			return Ok(lista);
		}
	}
}

