using System.Data.SqlClient;
using ApiLinaAgbd.Data;

namespace ApiLinaAgbd.Repositories.Facturacion.Documentos
{
	public class DocumentoFacturacionRepository : IDocumentoFacturacionRepository
	{
		private readonly Conexion _conexion;

		public DocumentoFacturacionRepository(Conexion conexion) => _conexion = conexion;

		public SqlConnection CreateConnection() => _conexion.ObtenerConexion();
	}
}
