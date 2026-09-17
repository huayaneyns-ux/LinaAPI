using ApiLinaAgbd.Models.Facturacion.NotaDebito;
using ApiLinaAgbd.Services.Facturacion.NotaDebito;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Facturacion.NotaDebito
{
	[ApiController]
	[Route("api/facturacion")]
	[Tags("Facturacion - Nota Debito (ND)")]
	public class NotaDebitoController : ControllerBase
	{
		private readonly INotaDebitoService _notaDebitoService;

		public NotaDebitoController(INotaDebitoService notaDebitoService)
		{
			_notaDebitoService = notaDebitoService;
		}

		[HttpGet("comprobantes/notas/debito/bases")]
		public async Task<IActionResult> ListarBases()
		{
			return Ok(await _notaDebitoService.ListarBasesAsync());
		}

		[HttpPost("comprobantes/notas/debito")]
		public IActionResult EmitirNotaDebito([FromBody] NotaDebitoEmitirRequestDto request) =>
			BadRequest(new { mensaje = "La emisión de notas de débito está deshabilitada." });
	}
}
