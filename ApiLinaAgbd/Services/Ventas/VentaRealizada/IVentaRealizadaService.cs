using ApiLinaAgbd.Models.Ventas.VentasRealizadas;

namespace ApiLinaAgbd.Services.Ventas.VentaRealizada
{
	public interface IVentaRealizadaService
	{
		List<VentaRealizadaSelectDto> Listar();
		VentaRealizadaSelectDto? Obtener(int id);
		List<VentaRealizadaDetalleDto> Detalle(int id);
		List<VentaRealizadaPagoDto> Pago(int id);
	}
}
