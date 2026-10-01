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
	public IActionResult Listar()
	{
		return Ok(_auditoriaRepository.Listar());
	}
}
