using System.Data.SqlClient;

namespace ApiLinaAgbd.Data
{
	public class Conexion
	{
		private readonly string cadena;

		public Conexion(IConfiguration config)
		{
			cadena = config.GetConnectionString("conexion");
		}

		public SqlConnection ObtenerConexion()
		{
			return new SqlConnection(cadena);
		}

		internal SqlConnection GetConnection()
		{
			throw new NotImplementedException();
		}
	}
}