using ApiLinaAgbd.Models.Ventas.PedidosRecibidos;
using ApiLinaAgbd.Repositories.Ventas.PedidosRecibidos;

namespace ApiLinaAgbd.Services.Ventas.PedidosRecibidos
{
	public class PedidosRecibidosService : IPedidosRecibidosService
	{
		private readonly IPedidosRecibidosRepository _pedidosRecibidosRepository;

		public PedidosRecibidosService(IPedidosRecibidosRepository pedidosRecibidosRepository)
		{
			_pedidosRecibidosRepository = pedidosRecibidosRepository;
		}

		public int InsertarPedido(PedidoInsertDto modelo)
		{
			return _pedidosRecibidosRepository.InsertarPedido(modelo);
		}

		public void CambiarEstado(PedidoUpdateEstadoDto modelo)
		{
			_pedidosRecibidosRepository.CambiarEstado(modelo);
		}

		public PedidoSelectIdDto? ObtenerPedido(int id)
		{
			return _pedidosRecibidosRepository.ObtenerPedido(id);
		}

		public List<PedidoSelectDto> ListarPedidos()
		{
			return _pedidosRecibidosRepository.ListarPedidos();
		}
	}
}
