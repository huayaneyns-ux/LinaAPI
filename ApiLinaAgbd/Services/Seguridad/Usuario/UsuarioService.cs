using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Repositories.Seguridad.Usuario;
using ApiLinaAgbd.Services.Seguridad.Auth;

namespace ApiLinaAgbd.Services.Seguridad.Usuario
{
	public class UsuarioService : IUsuarioService
	{
		private readonly IUsuarioRepository _usuarioRepository;
		private readonly IAuthService _authService;

		public UsuarioService(IUsuarioRepository usuarioRepository, IAuthService authService)
		{
			_usuarioRepository = usuarioRepository;
			_authService = authService;
		}

		public UsuarioLoginResponseDto? Login(UsuarioLoginDto modelo)
		{
			return _authService.Login(modelo);
		}

		public List<UsuarioSelectDto> Listar()
		{
			return _usuarioRepository.Listar();
		}

		public UsuarioSelectDto? Obtener(int id)
		{
			return _usuarioRepository.Obtener(id);
		}

		public int Guardar(UsuarioInsertUpdateDto modelo)
		{
			return _usuarioRepository.Guardar(modelo);
		}

		public void Eliminar(int id)
		{
			_usuarioRepository.Eliminar(id);
		}
	}
}
