using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Repositories.Seguridad.Auth;

namespace ApiLinaAgbd.Services.Seguridad.Auth
{
	public class AuthService : IAuthService
	{
		private readonly IAuthRepository _authRepository;

		public AuthService(IAuthRepository authRepository)
		{
			_authRepository = authRepository;
		}

		public UsuarioLoginResponseDto? Login(UsuarioLoginDto modelo)
		{
			return _authRepository.Login(modelo);
		}
	}
}
