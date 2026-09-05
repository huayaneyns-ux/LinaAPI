using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Ventas;

namespace ApiLinaAgbd.Repositories.Ventas.Lugares
{
	public class LugaresRepository : ILugaresRepository
	{
		private readonly Conexion _conexion;

		public LugaresRepository(Conexion conexion)
		{
			_conexion = conexion;
		}

		public List<DepartamentoDto> ListarDepartamentos()
		{
			var lista = new List<DepartamentoDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DEP_SEL_DEPARTAMENTO_LISTAR",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new DepartamentoDto
					{
						id = Convert.ToInt32(dr["id"]),
						nombre = dr["nombre"].ToString() ?? string.Empty
					});
				}

				dr.Close();
			}

			return lista;
		}

		public List<ProvinciaDto> ListarProvincias(int idDepartamento)
		{
			var lista = new List<ProvinciaDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_PROV_SEL_PROVINCIA_LISTAR",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdDepartamento",
					idDepartamento);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new ProvinciaDto
					{
						id = Convert.ToInt32(dr["id"]),
						nombre = dr["nombre"].ToString() ?? string.Empty,
						idDepartamento = Convert.ToInt32(dr["idDepartamento"])
					});
				}

				dr.Close();
			}

			return lista;
		}

		public List<DistritoDto> ListarDistritos(int idProvincia)
		{
			var lista = new List<DistritoDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DIS_SEL_DISTRITO_LISTAR",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdProvincia",
					idProvincia);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new DistritoDto
					{
						id = Convert.ToInt32(dr["id"]),
						nombre = dr["nombre"].ToString() ?? string.Empty,
						idProvincia = Convert.ToInt32(dr["idProvincia"])
					});
				}

				dr.Close();
			}

			return lista;
		}

		public List<DireccionDto> ListarDireccionesUsuario(int idUsuario)
		{
			var lista = new List<DireccionDto>();

			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DIR_SEL_DIRECCION_USUARIO",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdUsuario",
					idUsuario);

				SqlDataReader dr = cmd.ExecuteReader();

				while (dr.Read())
				{
					lista.Add(new DireccionDto
					{
						id = Convert.ToInt32(dr["id"]),

						nombreDireccion = dr["nombre_direccion"].ToString()
							?? string.Empty,

						referencia = dr["referencia"].ToString()
							?? string.Empty,

						idDistrito = Convert.ToInt32(dr["id_distrito"]),

						distrito = dr["distrito"].ToString()
							?? string.Empty,

						idProvincia = Convert.ToInt32(dr["id_provincia"]),

						provincia = dr["provincia"].ToString()
							?? string.Empty,

						idDepartamento = Convert.ToInt32(dr["id_departamento"]),

						departamento = dr["departamento"].ToString()
							?? string.Empty,

						esPrincipal = Convert.ToBoolean(dr["es_principal"])
					});
				}

				dr.Close();
			}

			return lista;
		}

		public void InsertarDireccion(DireccionInsertDto modelo)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DIR_INS_DIRECCION_USUARIO",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdUsuario",
					modelo.idUsuario);

				cmd.Parameters.AddWithValue(
					"@NombreDireccion",
					modelo.nombreDireccion);

				cmd.Parameters.AddWithValue(
					"@Referencia",
					modelo.referencia);

				cmd.Parameters.AddWithValue(
					"@IdDistrito",
					modelo.idDistrito);

				cmd.Parameters.AddWithValue(
					"@EsPrincipal",
					modelo.esPrincipal);

				cmd.ExecuteNonQuery();
			}
		}

		public void CambiarPrincipal(DireccionPrincipalDto modelo)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DIR_UPD_CAMBIAR_PRINCIPAL",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdUsuario",
					modelo.idUsuario);

				cmd.Parameters.AddWithValue(
					"@IdDireccion",
					modelo.idDireccion);

				cmd.ExecuteNonQuery();
			}
		}

		public void EliminarDireccion(int idUsuario, int idDireccion)
		{
			using (SqlConnection con = _conexion.ObtenerConexion())
			{
				con.Open();

				SqlCommand cmd = new SqlCommand(
					"USP_DIR_DEL_DIRECCION_USUARIO",
					con);

				cmd.CommandType = CommandType.StoredProcedure;

				cmd.Parameters.AddWithValue(
					"@IdUsuario",
					idUsuario);

				cmd.Parameters.AddWithValue(
					"@IdDireccion",
					idDireccion);

				cmd.ExecuteNonQuery();
			}
		}
	}
}
