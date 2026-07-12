using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Compras.Proveedor;

namespace ApiLinaAgbd.Controllers.Inventario
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProveedorController : ControllerBase
	{
		private readonly Conexion _conexion;

		public ProveedorController(Conexion conexion)
		{
			_conexion = conexion;
		}

		// =====================================
		// LISTAR
		// =====================================
		[HttpGet]
		public IActionResult Listar()
		{
			var lista = new List<object>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_PROVEEDOR_LISTAR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new
					{
						Id = Convert.ToInt32(dr["id"]),
						Ruc = dr["ruc"].ToString(),
						RazonSocial = dr["razon_social"].ToString(),
						NombreContacto = dr["nombre_contacto"].ToString(),
						Telefono = dr["telefono"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"]),

						IdDireccion = Convert.ToInt32(dr["id_direccion"]),
						Direccion = dr["direccion"].ToString(),
						Distrito = dr["distrito"].ToString(),
						Provincia = dr["provincia"].ToString(),
						Departamento = dr["departamento"].ToString()
					});
				}
			}

			return Ok(lista);
		}

		// =====================================
		// OBTENER POR ID
		// =====================================
		[HttpGet("{id}")]
		public IActionResult Obtener(int id)
		{
			object proveedor = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_PROVEEDOR_OBTENER", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				SqlDataReader dr = cmd.ExecuteReader();

				if (dr.Read())
				{
					proveedor = new
					{
						Id = Convert.ToInt32(dr["id"]),
						Ruc = dr["ruc"].ToString(),
						RazonSocial = dr["razon_social"].ToString(),
						NombreContacto = dr["nombre_contacto"].ToString(),
						Telefono = dr["telefono"].ToString(),
						Estado = Convert.ToBoolean(dr["estado"]),

						IdDireccion = Convert.ToInt32(dr["id_direccion"]),
						Direccion = dr["direccion"].ToString(),
						Distrito = dr["distrito"].ToString(),
						Provincia = dr["provincia"].ToString(),
						Departamento = dr["departamento"].ToString()
					};
				}
			}

			if (proveedor == null)
				return NotFound("Proveedor no encontrado");

			return Ok(proveedor);
		}

		// =====================================
		// INSERTAR
		// =====================================
		[HttpPost]
		public IActionResult Insertar([FromBody] dynamic data)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_INS_PROVEEDOR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Ruc", (string)data.ruc);
				cmd.Parameters.AddWithValue("@RazonSocial", (string)data.razonSocial);
				cmd.Parameters.AddWithValue("@NombreContacto", (string)data.nombreContacto);
				cmd.Parameters.AddWithValue("@Telefono", (string)data.telefono);
				cmd.Parameters.AddWithValue("@IdDireccion", (int)data.idDireccion);

				cmd.ExecuteNonQuery();
			}

			return Ok("Proveedor registrado correctamente");
		}

		// =====================================
		// ACTUALIZAR
		// =====================================
		[HttpPut]
		public IActionResult Actualizar([FromBody] ProveedorUpdate proveedor)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_UPD_PROVEEDOR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", proveedor.Id);
				cmd.Parameters.AddWithValue("@Ruc", proveedor.Ruc);
				cmd.Parameters.AddWithValue("@RazonSocial", proveedor.RazonSocial);
				cmd.Parameters.AddWithValue("@NombreContacto", proveedor.NombreContacto);
				cmd.Parameters.AddWithValue("@Telefono", proveedor.Telefono);
				cmd.Parameters.AddWithValue("@IdDireccion", proveedor.IdDireccion);
				cmd.Parameters.AddWithValue("@Estado", proveedor.Estado);

				cmd.ExecuteNonQuery();
			}

			return Ok("Proveedor actualizado correctamente");
		}

		// =====================================
		// ELIMINAR (SOFT DELETE)
		// =====================================
		[HttpDelete("{id}")]
		public IActionResult Eliminar(int id)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_DEL_PROVEEDOR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				cmd.ExecuteNonQuery();
			}

			return Ok("Proveedor desactivado correctamente");
		}
	}
}