using ApiLinaAgbd.Models.Persona;

namespace ApiLinaAgbd.Repositories.Persona
{
	public interface IPersonaRepository
	{
		PersonaData? Buscar(string tipoDocumento, string numero);
		int Registrar(string tipoDocumento, string numero, string nombreApellido, string? direccion, string? ubigeo);
	}
}
