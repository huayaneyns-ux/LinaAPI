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

		[HttpPost("comprobantes/notas/debito")]
		public async Task<IActionResult> EmitirNotaDebito([FromBody] NotaDebitoEmitirRequestDto request)
		{
			try
			{
				var resultado = await _notaDebitoService.EmitirAsync(request);
				return Ok(resultado);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { mensaje = ex.Message });
			}
		}
	}
}
