using ApiLinaAgbd.Models.Ventas.VentasRealizadas;
using ApiLinaAgbd.Repositories.Ventas.VentaRealizada;

namespace ApiLinaAgbd.Services.Ventas.VentaRealizada
{
	public class VentaRealizadaService : IVentaRealizadaService
	{
		private readonly IVentaRealizadaRepository _ventaRealizadaRepository;

		public VentaRealizadaService(IVentaRealizadaRepository ventaRealizadaRepository)
		{
			_ventaRealizadaRepository = ventaRealizadaRepository;
		}

		public List<VentaRealizadaSelectDto> Listar()
		{
			return _ventaRealizadaRepository.Listar();
		}

		public VentaRealizadaSelectDto? Obtener(int id)
		{
			return _ventaRealizadaRepository.Obtener(id);
		}

		public List<VentaRealizadaDetalleDto> Detalle(int id)
		{
			return _ventaRealizadaRepository.Detalle(id);
		}

		public List<VentaRealizadaPagoDto> Pago(int id)
		{
			return _ventaRealizadaRepository.Pago(id);
		}
	}
}
