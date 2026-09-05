using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Compras.Proveedor;

namespace ApiLinaAgbd.Repositories.Compras.Proveedor
{
	public class ProveedorRepository : IProveedorRepository
	{
		private readonly Conexion _conexion;

		public ProveedorRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public List<ProveedorSelectDto> Listar()
		{
			var lista = new List<ProveedorSelectDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_PROVEEDOR_LISTAR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new ProveedorSelectDto
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

			return lista;
		}

		public ProveedorSelectDto? Obtener(int id)
		{
			ProveedorSelectDto? proveedor = null;

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_SEL_PROVEEDOR_OBTENER", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				SqlDataReader dr = cmd.ExecuteReader();

				if (dr.Read())
				{
					proveedor = new ProveedorSelectDto
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

			return proveedor;
		}

		public void Insertar(JsonElement json)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				string ruc = json.GetProperty("ruc").GetString()!;
				string razonSocial = json.GetProperty("razonSocial").GetString()!;
				string nombreContacto = json.GetProperty("nombreContacto").GetString()!;
				string telefono = json.GetProperty("telefono").GetString()!;
				int idDireccion = json.GetProperty("idDireccion").GetInt32();

				SqlCommand cmd = new SqlCommand("USP_PRO_INS_PROVEEDOR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Ruc", ruc);
				cmd.Parameters.AddWithValue("@RazonSocial", razonSocial);
				cmd.Parameters.AddWithValue("@NombreContacto", nombreContacto);
				cmd.Parameters.AddWithValue("@Telefono", telefono);
				cmd.Parameters.AddWithValue("@IdDireccion", idDireccion);

				cmd.ExecuteNonQuery();
			}
		}

		public void Actualizar(ProveedorUpdate proveedor)
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
		}

		public void Eliminar(int id)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand("USP_PRO_DEL_PROVEEDOR", con);
				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue("@Id", id);

				cmd.ExecuteNonQuery();
			}
		}
	}
}
