using ApiLinaAgbd.Models.Facturacion.NotaDebito;
using ApiLinaAgbd.Models.Facturacion.Notas;

namespace ApiLinaAgbd.Services.Facturacion.NotaDebito
{
	public interface INotaDebitoService
	{
		Task<List<NotaComprobanteBaseDisponibleDto>> ListarBasesAsync();
		Task<NotaComprobanteResultadoDto> EmitirAsync(NotaDebitoEmitirRequestDto request);
	}
}
