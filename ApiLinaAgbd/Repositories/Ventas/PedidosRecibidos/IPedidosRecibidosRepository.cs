using ApiLinaAgbd.Models.Ventas.PedidosRecibidos;

namespace ApiLinaAgbd.Repositories.Ventas.PedidosRecibidos
{
	public interface IPedidosRecibidosRepository
	{
		int InsertarPedido(PedidoInsertDto modelo);
		void CambiarEstado(PedidoUpdateEstadoDto modelo);
		PedidoSelectIdDto? ObtenerPedido(int id);
		List<PedidoSelectDto> ListarPedidos();
	}
}
