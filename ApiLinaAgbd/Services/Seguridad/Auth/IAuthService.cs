using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Services.Seguridad.Auth
{
	public interface IAuthService
	{
		UsuarioLoginResponseDto? Login(UsuarioLoginDto modelo);
	}
}
