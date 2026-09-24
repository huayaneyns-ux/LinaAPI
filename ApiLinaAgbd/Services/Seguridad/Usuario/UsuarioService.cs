using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Repositories.Seguridad.Usuario;
using ApiLinaAgbd.Services.Seguridad.Auth;
using ApiLinaAgbd.Services.Integracion;

namespace ApiLinaAgbd.Services.Seguridad.Usuario
{
	public class UsuarioService : IUsuarioService
	{
		private readonly IUsuarioRepository _usuarioRepository;
		private readonly IAuthService _authService;
		private readonly IIntegracionClientesService _integracionClientesService;

		public UsuarioService(IUsuarioRepository usuarioRepository, IAuthService authService, IIntegracionClientesService integracionClientesService)
		{
			_usuarioRepository = usuarioRepository;
			_authService = authService;
			_integracionClientesService = integracionClientesService;
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
			var documento = modelo.dni?.Trim();
			if (!string.IsNullOrWhiteSpace(documento))
			{
				var tipoDocumento = modelo.tipoDocumento.Trim().ToUpperInvariant();
				var usuarioConDocumento = _usuarioRepository.ObtenerPorDocumento(tipoDocumento, documento);
				if (usuarioConDocumento is not null && usuarioConDocumento.id != modelo.idUsuario)
				{
					throw new UsuarioDuplicadoException("dni", "Ya existe una cuenta con ese DNI/documento.");
				}
			}

			var correo = modelo.correo?.Trim() ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(correo))
			{
				var usuarioConCorreo = _usuarioRepository.ObtenerPorCorreo(correo);
				if (usuarioConCorreo is not null && usuarioConCorreo.id != modelo.idUsuario)
				{
					throw new UsuarioDuplicadoException("correo", "Ya existe una cuenta con ese correo electrónico.");
				}
			}

			if (modelo.idRol == 1 && string.IsNullOrWhiteSpace(modelo.origen))
			{
				modelo.origen = "lina";
			}

			var id = _usuarioRepository.Guardar(modelo);
			if (modelo.idUsuario is null && modelo.idRol == 1)
			{
				_ = _integracionClientesService.NotificarClienteNuevoAsync(new Models.Integracion.ClienteIntegracionDto
				{
					id = id,
					nombre = modelo.nombreApellido,
					documento = modelo.dni,
					telefono = modelo.telefono,
					email = modelo.correo
				});
			}
			return id;
		}

		public void Eliminar(int id)
		{
			_usuarioRepository.Eliminar(id);
		}
	}
}
