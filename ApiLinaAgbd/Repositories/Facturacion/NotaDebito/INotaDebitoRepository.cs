using System.Data.SqlClient;
using ApiLinaAgbd.Models.Facturacion.Notas;

namespace ApiLinaAgbd.Repositories.Facturacion.NotaDebito
{
	public interface INotaDebitoRepository
	{
		SqlConnection CreateConnection();
		Task<List<NotaComprobanteBaseDisponibleDto>> ListarComprobantesBaseAsync();
	}
}
