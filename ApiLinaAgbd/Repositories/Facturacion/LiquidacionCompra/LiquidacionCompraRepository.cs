using System.Data.SqlClient;
using ApiLinaAgbd.Data;

namespace ApiLinaAgbd.Repositories.Facturacion.LiquidacionCompra
{
	public class LiquidacionCompraRepository : ILiquidacionCompraRepository
	{
		private readonly Conexion _conexion;

		public LiquidacionCompraRepository(Conexion conexion) => _conexion = conexion;

		public SqlConnection CreateConnection() => _conexion.ObtenerConexion();
	}
}
