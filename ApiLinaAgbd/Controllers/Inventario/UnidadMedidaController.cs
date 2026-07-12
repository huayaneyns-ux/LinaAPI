using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Inventario.UnidadMedida;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class UnidadMedidaController : ControllerBase
	{
		private readonly Conexion _conexion;

		public UnidadMedidaController(Conexion conexion)
		{
			_conexion = conexion;
		}


		//=========================================
		// LISTAR
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarUnidadMedidas()
		{
			List<UnidadMedidaSelectDto> lista = new();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_SEL_UNIDAD_MEDIDA", con);
				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new UnidadMedidaSelectDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Nombre = dr["nombre"].ToString(),
						Abreviatura = dr["abreviatura"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"])
					});
				}
			}

			return Ok(lista);
		}



		//=========================================
		// INSERTAR
		//=========================================
		[HttpPost]
		public IActionResult InsertarUnidadMedida(
			[FromBody] UnidadMedidaInsertDto unidad)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_INS_UNIDAD_MEDIDA",
					con);

				cmd.CommandType = CommandType.StoredProcedure;


				cmd.Parameters.AddWithValue(
					"@nombre",
					unidad.Nombre);


				cmd.Parameters.AddWithValue(
					"@abreviatura",
					unidad.Abreviatura);


				cmd.ExecuteNonQuery();
			}


			return Ok("Unidad de medida registrada correctamente.");
		}





		//=========================================
		// ACTUALIZAR
		//=========================================
		[HttpPut]
		public IActionResult ActualizarUnidadMedida(
			[FromBody] UnidadMedidaUpdateDto unidad)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_UPD_UNIDAD_MEDIDA",
					con);


				cmd.CommandType = CommandType.StoredProcedure;


				cmd.Parameters.AddWithValue(
					"@id",
					unidad.Id);


				cmd.Parameters.AddWithValue(
					"@nombre",
					unidad.Nombre);


				cmd.Parameters.AddWithValue(
					"@abreviatura",
					unidad.Abreviatura);



				cmd.ExecuteNonQuery();
			}


			return Ok("Unidad de medida actualizada correctamente.");
		}





		//=========================================
		// ELIMINAR
		//=========================================
		[HttpDelete("{id}")]
		public IActionResult EliminarUnidadMedida(int id)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_DEL_UNIDAD_MEDIDA",
					con);


				cmd.CommandType = CommandType.StoredProcedure;


				cmd.Parameters.AddWithValue(
					"@id",
					id);


				cmd.ExecuteNonQuery();
			}


			return Ok("Unidad de medida eliminada correctamente.");
		}





		//=========================================
		// OBTENER POR ID
		//=========================================
		[HttpGet("{id}")]
		public IActionResult ObtenerUnidadMedidaPorId(int id)
		{

			UnidadMedidaObtenerIdDto unidad = null;


			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();


				SqlCommand cmd = new SqlCommand(
					"USP_SEL_UNIDAD_MEDIDA_ID",
					con);


				cmd.CommandType = CommandType.StoredProcedure;


				cmd.Parameters.AddWithValue(
					"@id",
					id);



				SqlDataReader dr = cmd.ExecuteReader();


				if (dr.Read())
				{
					unidad = new UnidadMedidaObtenerIdDto
					{
						Id = Convert.ToInt32(dr["id"]),
						Nombre = dr["nombre"].ToString(),
						Abreviatura = dr["abreviatura"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"])
					};
				}
			}


			if (unidad == null)
				return NotFound("Unidad de medida no encontrada.");


			return Ok(unidad);
		}
	}
}
