using ApiLinaAgbd.Repositories.Seguridad.Auditoria;
using Microsoft.AspNetCore.Mvc;

namespace ApiLinaAgbd.Controllers.Seguridad;

[ApiController]
[Route("api/[controller]")]
public sealed class AuditoriaController : ControllerBase
{
	private readonly IAuditoriaRepository _auditoriaRepository;

	public AuditoriaController(IAuditoriaRepository auditoriaRepository)
	{
		_auditoriaRepository = auditoriaRepository;
	}

	[HttpGet("Lista")]
	public IActionResult Listar(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 10,
		[FromQuery] string? search = null,
		[FromQuery] string? sortBy = null,
		[FromQuery] string? sortDirection = null)
	{
		return Ok(_auditoriaRepository.Listar(page, pageSize, search, sortBy, sortDirection));
	}
}
