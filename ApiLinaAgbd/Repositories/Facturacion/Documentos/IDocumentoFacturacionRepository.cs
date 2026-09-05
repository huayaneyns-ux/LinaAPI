using System.Data.SqlClient;

namespace ApiLinaAgbd.Repositories.Facturacion.Documentos
{
	public interface IDocumentoFacturacionRepository
	{
		SqlConnection CreateConnection();
	}
}
