using System.Data.SqlClient;
using ApiLinaAgbd.Data;

namespace ApiLinaAgbd.Repositories.Facturacion.NotaCredito
{
	public class NotaCreditoRepository : INotaCreditoRepository
	{
		private readonly Conexion _conexion;

		public NotaCreditoRepository(Conexion conexion) => _conexion = conexion;

		public SqlConnection CreateConnection() => _conexion.ObtenerConexion();
	}
}
