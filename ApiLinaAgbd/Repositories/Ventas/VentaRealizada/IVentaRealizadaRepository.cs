using ApiLinaAgbd.Models.Ventas.VentasRealizadas;

namespace ApiLinaAgbd.Repositories.Ventas.VentaRealizada
{
	public interface IVentaRealizadaRepository
	{
		List<VentaRealizadaSelectDto> Listar();
		VentaRealizadaSelectDto? Obtener(int id);
		List<VentaRealizadaDetalleDto> Detalle(int id);
		List<VentaRealizadaPagoDto> Pago(int id);
	}
}
