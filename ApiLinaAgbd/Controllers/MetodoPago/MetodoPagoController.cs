using ApiLinaAgbd.Services.MetodoPago;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.MetodoPago
{
	[ApiController]
	[Route("api/[controller]")]
	public class MetodoPagoController : ControllerBase
	{
		private readonly IMetodoPagoService _metodoPagoService;

		public MetodoPagoController(IMetodoPagoService metodoPagoService)
		{
			_metodoPagoService = metodoPagoService;
		}

		//=========================================
		// LISTAR METODOS DE PAGO
		//=========================================
		[HttpGet("Lista")]
		public IActionResult Listar()
		{
			var lista = _metodoPagoService.Listar();
			return Ok(lista);
		}
	}
}
