using System.Data.SqlClient;

namespace ApiLinaAgbd.Repositories.Facturacion.NotaDebito
{
	public interface INotaDebitoRepository
	{
		SqlConnection CreateConnection();
	}
}
