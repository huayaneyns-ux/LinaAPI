using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProductoController : ControllerBase
	{
		private readonly Conexion _conexion;

		public ProductoController(Conexion conexion)
		{
			_conexion = conexion;
		}

		[HttpGet]
		public IActionResult ObtenerProductos()
		{
			var lista = new List<object>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("SELECT * FROM VW_PRODUCTOS", con);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new
					{
						id = dr["id"],
						codigo = dr["codigo"],
						nombre = dr["nombreProducto"],
						idCategoria = dr["idCategoria"],
						categoria = dr["categoria"],
						descripcion = dr["descripcion"],
						precio = dr["precio"],
						url = dr["imagenUrl"]
					});
				}
			}

			return Ok(lista);
		}
	}
}

