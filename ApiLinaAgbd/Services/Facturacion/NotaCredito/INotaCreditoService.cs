using ApiLinaAgbd.Models.Facturacion.NotaCredito;
using ApiLinaAgbd.Models.Facturacion.Notas;

namespace ApiLinaAgbd.Services.Facturacion.NotaCredito
{
	public interface INotaCreditoService
	{
		Task<List<NotaComprobanteBaseDisponibleDto>> ListarComprobantesBaseAsync();
		Task<NotaComprobanteResultadoDto> EmitirAsync(NotaCreditoEmitirRequestDto request);
	}
}
