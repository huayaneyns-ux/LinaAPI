using ApiLinaAgbd.Models.Ventas.Caja;

namespace ApiLinaAgbd.Repositories.Ventas.Caja
{
	public interface ICajaRepository
	{
		int RegistrarVenta(CajaVentaInsertDto venta);
		CajaClienteDto? BuscarCliente(string dni);
		CajaClienteDto? BuscarClientePorDocumento(string tipoDocumento, string numero);
		int CrearOReutilizarCliente(CajaClienteInsertDto cliente);
		void RegistrarPago(int id, CajaPagoInsertDto pago);
	}
}
