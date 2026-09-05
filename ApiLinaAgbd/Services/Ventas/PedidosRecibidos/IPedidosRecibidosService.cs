using ApiLinaAgbd.Models.Ventas.PedidosRecibidos;

namespace ApiLinaAgbd.Services.Ventas.PedidosRecibidos
{
	public interface IPedidosRecibidosService
	{
		int InsertarPedido(PedidoInsertDto modelo);
		void CambiarEstado(PedidoUpdateEstadoDto modelo);
		PedidoSelectIdDto? ObtenerPedido(int id);
		List<PedidoSelectDto> ListarPedidos();
	}
}
