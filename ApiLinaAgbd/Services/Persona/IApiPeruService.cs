using ApiLinaAgbd.Models.ApiPeru;

namespace ApiLinaAgbd.Services.Persona
{
	public interface IApiPeruService
	{
		Task<PersonaResponseDto> ConsultarYRegistrarPersonaAsync(string tipoDocumento, string numero);
	}
}
