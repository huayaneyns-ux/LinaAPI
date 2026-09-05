using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Models.Facturacion.Documentos;
using ApiLinaAgbd.Models.Facturacion.SunatTransmission;

namespace ApiLinaAgbd.Services.Facturacion.Documentos
{
	public interface IDocumentoFacturacionService
	{
		Task<List<ComprobanteVentaListItemDto>> ListarTodosAsync();
		Task<ComprobanteVentaListItemDto> ObtenerDetalleAsync(string id);
		Task<DocumentoFacturacionDto> ObtenerPorIdAsync(string id);
		Task<DocumentoFacturacionDto> SincronizarEstadoSunatAsync(string id);
		Task<(byte[] Content, string ContentType, string FileName)> DescargarPdfAsync(string id, string format);
		Task<DocumentoFacturacionDto> AnularAsync(string id, string reason);
		Task<ComprobanteVentaListItemDto> ReenviarAsync(string id);
		Task<List<SunatTransmissionDto>> ListarTransmisionesSunatAsync();
	}
}
