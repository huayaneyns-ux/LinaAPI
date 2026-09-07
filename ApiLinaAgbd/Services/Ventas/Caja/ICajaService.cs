using ApiLinaAgbd.Models.Ventas.Caja;

namespace ApiLinaAgbd.Services.Ventas.Caja
{
	public interface ICajaService
	{
		int RegistrarVenta(CajaVentaInsertDto venta);
		CajaClienteDto? BuscarCliente(string dni);
		Task<CajaClienteDto?> BuscarClientePorDocumentoAsync(string tipoDocumento, string numero);
		Task<CajaClienteDto?> CrearClienteAsync(CajaClienteInsertDto cliente);
		void RegistrarPago(int id, CajaPagoInsertDto pago);
	}
}
