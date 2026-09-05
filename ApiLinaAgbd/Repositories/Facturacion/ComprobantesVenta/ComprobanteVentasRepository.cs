using System.Data.SqlClient;
using ApiLinaAgbd.Data;

namespace ApiLinaAgbd.Repositories.Facturacion.ComprobantesVenta
{
	public class ComprobanteVentasRepository : IComprobanteVentasRepository
	{
		private readonly Conexion _conexion;

		public ComprobanteVentasRepository(Conexion conexion) => _conexion = conexion;

		public SqlConnection CreateConnection() => _conexion.ObtenerConexion();
	}
}
