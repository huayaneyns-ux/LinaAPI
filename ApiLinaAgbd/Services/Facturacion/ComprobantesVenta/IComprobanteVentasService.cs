using ApiLinaAgbd.Models.Facturacion.ComprobantesVenta;

namespace ApiLinaAgbd.Services.Facturacion.ComprobantesVenta
{
	public interface IComprobanteVentasService
	{
		Task<List<VentaComprobanteDisponibleDto>> ListarVentasDisponiblesAsync();
		Task<List<ComprobanteVentaListItemDto>> ListarComprobantesAsync();
		Task<ComprobanteVentaListItemDto> ObtenerComprobantePorIdAsync(string id);
		Task<ComprobanteVentaListItemDto> SincronizarEstadoSunatAsync(string id);
		Task<(byte[] Content, string ContentType, string FileName)> DescargarPdfAsync(string id, string format);
		Task<ComprobanteVentaListItemDto> AnularAsync(string id, string reason);
		Task<ComprobanteVentaListItemDto> EmitirAsync(ComprobanteVentaEmitirRequestDto request);
	}
}
