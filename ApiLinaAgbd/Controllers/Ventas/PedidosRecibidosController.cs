using ApiLinaAgbd.Models.Ventas.PedidosRecibidos;
using ApiLinaAgbd.Services.Ventas.PedidosRecibidos;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Ventas
{
	[ApiController]
	[Route("api/[controller]")]
	public class PedidosRecibidosController : ControllerBase
	{
		private readonly IPedidosRecibidosService _pedidosRecibidosService;

		public PedidosRecibidosController(IPedidosRecibidosService pedidosRecibidosService)
		{
			_pedidosRecibidosService = pedidosRecibidosService;
		}

		//=========================================
		// INSERTAR PEDIDO
		//=========================================
		[HttpPost("Insertar")]
		public IActionResult InsertarPedido(PedidoInsertDto modelo)
		{
			try
			{
				int idPedido = _pedidosRecibidosService.InsertarPedido(modelo);

				return Ok(new
				{
					success = true,
					mensaje = "Pedido registrado correctamente.",
					idPedido
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					success = false,
					mensaje = ex.Message
				});
			}
		}

		//=========================================
		// CAMBIAR ESTADO PEDIDO
		//=========================================
		[HttpPut("CambiarEstado")]
		public IActionResult CambiarEstado(PedidoUpdateEstadoDto modelo)
		{
			_pedidosRecibidosService.CambiarEstado(modelo);

			return Ok(new
			{
				success = true,
				mensaje = "Estado del pedido actualizado correctamente."
			});
		}

		//=========================================
		// OBTENER PEDIDO POR ID
		//=========================================
		[HttpGet("{id}")]
		public IActionResult ObtenerPedido(int id)
		{
			var pedido = _pedidosRecibidosService.ObtenerPedido(id);

			if (pedido == null)
				return NotFound();

			return Ok(pedido);
		}

		//=========================================
		// LISTAR PEDIDOS
		//=========================================
		[HttpGet("Lista")]
		public IActionResult ListarPedidos()
		{
			var lista = _pedidosRecibidosService.ListarPedidos();
			return Ok(lista);
		}
	}
}
