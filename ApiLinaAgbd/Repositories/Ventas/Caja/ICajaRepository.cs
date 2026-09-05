using ApiLinaAgbd.Models.Ventas.Caja;

namespace ApiLinaAgbd.Repositories.Ventas.Caja
{
	public interface ICajaRepository
	{
		int RegistrarVenta(CajaVentaInsertDto venta);
		CajaClienteDto? BuscarCliente(string dni);
		int CrearCliente(CajaClienteInsertDto cliente);
		void RegistrarPago(int id, CajaPagoInsertDto pago);
	}
}
