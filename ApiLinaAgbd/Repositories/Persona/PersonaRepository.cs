using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Persona;

namespace ApiLinaAgbd.Repositories.Persona
{
	public class PersonaRepository : IPersonaRepository
	{
		private readonly Conexion _conexion;
		private readonly ILogger<PersonaRepository> _logger;

		public PersonaRepository(Conexion conexion, ILogger<PersonaRepository> logger)
		{
			_conexion = conexion;
			_logger = logger;
		}

		public PersonaData? Buscar(string tipoDocumento, string numero)
		{
			try
			{
				using SqlConnection con = _conexion.ObtenerConexion();
				con.Open();

				using SqlCommand cmd = new("sp_BuscarNombrePersona", con);
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.AddWithValue("@tipoDocumento", tipoDocumento);
				cmd.Parameters.AddWithValue("@numero", numero);

				using SqlDataReader dr = cmd.ExecuteReader();
				if (dr.Read())
				{
					string num = dr["numero"] != DBNull.Value ? dr["numero"].ToString()?.Trim() ?? "" : "";
					string nom = dr["nombre"] != DBNull.Value ? dr["nombre"].ToString()?.Trim() ?? "" : "";

					if (!string.IsNullOrEmpty(num) && !string.IsNullOrEmpty(nom))
					{
						return new PersonaData { Numero = num, Nombre = nom };
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al ejecutar sp_BuscarNombrePersona con tipo {Tipo} y número {Numero}", tipoDocumento, numero);
			}

			return null;
		}

		public void Registrar(string tipoDocumento, string numero, string nombreApellido)
		{
			try
			{
				using SqlConnection con = _conexion.ObtenerConexion();
				con.Open();

				using SqlCommand cmd = new("sp_CrearDocumento", con);
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.AddWithValue("@tipoDocumento", tipoDocumento);
				cmd.Parameters.AddWithValue("@numero", numero);
				cmd.Parameters.AddWithValue("@nombre", nombreApellido);

				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al ejecutar sp_CrearDocumento para tipo {Tipo}, número {Numero}, nombre {Nombre}", tipoDocumento, numero, nombreApellido);
			}
		}
	}
}
