using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Repositories.Seguridad.Auth
{
	public interface IAuthRepository
	{
		UsuarioLoginResponseDto? Login(UsuarioLoginDto modelo);
	}
}
