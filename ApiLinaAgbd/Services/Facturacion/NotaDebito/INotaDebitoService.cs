using ApiLinaAgbd.Models.Facturacion.NotaDebito;
using ApiLinaAgbd.Models.Facturacion.Notas;

namespace ApiLinaAgbd.Services.Facturacion.NotaDebito
{
	public interface INotaDebitoService
	{
		Task<NotaComprobanteResultadoDto> EmitirAsync(NotaDebitoEmitirRequestDto request);
	}
}
