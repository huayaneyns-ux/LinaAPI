using ApiLinaAgbd.Models.Persona;

namespace ApiLinaAgbd.Repositories.Persona
{
	public interface IPersonaRepository
	{
		PersonaData? Buscar(string tipoDocumento, string numero);
		void Registrar(string tipoDocumento, string numero, string nombreApellido);
	}
}
