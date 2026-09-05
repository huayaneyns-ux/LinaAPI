using ApiLinaAgbd.Models.Ventas.Caja;

namespace ApiLinaAgbd.Services.Ventas.Caja
{
	public interface ICajaService
	{
		int RegistrarVenta(CajaVentaInsertDto venta);
		CajaClienteDto? BuscarCliente(string dni);
		int CrearCliente(CajaClienteInsertDto cliente);
		void RegistrarPago(int id, CajaPagoInsertDto pago);
	}
}
