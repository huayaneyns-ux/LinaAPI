using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Repositories.Seguridad.Auditoria;

public interface IAuditoriaRepository
{
	List<AuditoriaDto> Listar();
}
