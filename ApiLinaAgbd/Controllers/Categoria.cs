using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CategoriaController : ControllerBase
	{
		private readonly Conexion _conexion;

		public CategoriaController(Conexion conexion)
		{
			_conexion = conexion;
		}

		[HttpGet]
		public IActionResult ObtenerCategorias()
		{
			var lista = new List<object>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("SELECT * FROM Categoria WHERE estado = 1", con);

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

