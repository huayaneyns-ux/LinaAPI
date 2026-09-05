using System.Data.SqlClient;

namespace ApiLinaAgbd.Repositories.Facturacion.NotaCredito
{
	public interface INotaCreditoRepository
	{
		SqlConnection CreateConnection();
	}
}
