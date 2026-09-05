using System.Data.SqlClient;

namespace ApiLinaAgbd.Repositories.Facturacion.LiquidacionCompra
{
	public interface ILiquidacionCompraRepository
	{
		SqlConnection CreateConnection();
	}
}
