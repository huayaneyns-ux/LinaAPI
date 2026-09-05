using ApiLinaAgbd.Models.Ventas.Caja;
using ApiLinaAgbd.Repositories.Ventas.Caja;

namespace ApiLinaAgbd.Services.Ventas.Caja
{
	public class CajaService : ICajaService
	{
		private readonly ICajaRepository _cajaRepository;

		public CajaService(ICajaRepository cajaRepository)
		{
			_cajaRepository = cajaRepository;
		}

		public int RegistrarVenta(CajaVentaInsertDto venta)
		{
			return _cajaRepository.RegistrarVenta(venta);
		}

		public CajaClienteDto? BuscarCliente(string dni)
		{
			return _cajaRepository.BuscarCliente(dni);
		}

		public int CrearCliente(CajaClienteInsertDto cliente)
		{
			return _cajaRepository.CrearCliente(cliente);
		}

		public void RegistrarPago(int id, CajaPagoInsertDto pago)
		{
			_cajaRepository.RegistrarPago(id, pago);
		}
	}
}
