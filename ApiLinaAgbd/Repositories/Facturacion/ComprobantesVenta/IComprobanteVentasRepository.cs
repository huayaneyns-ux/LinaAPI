using System.Data.SqlClient;

namespace ApiLinaAgbd.Repositories.Facturacion.ComprobantesVenta
{
	public interface IComprobanteVentasRepository
	{
		SqlConnection CreateConnection();
	}
}
