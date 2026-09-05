using ApiLinaAgbd.Models.Facturacion.LiquidacionCompra;
using ApiLinaAgbd.Models.Facturacion.Notas;

namespace ApiLinaAgbd.Services.Facturacion.LiquidacionCompra
{
	public interface ILiquidacionCompraService
	{
		Task<List<LiquidacionCompraDisponibleDto>> ListarComprasDisponiblesAsync();
		Task<NotaComprobanteResultadoDto> EmitirAsync(LiquidacionCompraEmitirRequestDto request);
	}
}
