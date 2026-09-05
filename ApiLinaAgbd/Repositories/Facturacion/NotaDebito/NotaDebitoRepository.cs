using System.Data.SqlClient;
using ApiLinaAgbd.Data;

namespace ApiLinaAgbd.Repositories.Facturacion.NotaDebito
{
	public class NotaDebitoRepository : INotaDebitoRepository
	{
		private readonly Conexion _conexion;

		public NotaDebitoRepository(Conexion conexion) => _conexion = conexion;

		public SqlConnection CreateConnection() => _conexion.ObtenerConexion();
	}
}
