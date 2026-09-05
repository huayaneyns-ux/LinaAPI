using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Repositories.Seguridad.Rol;

namespace ApiLinaAgbd.Services.Seguridad.Rol
{
	public class RolService : IRolService
	{
		private readonly IRolRepository _rolRepository;

		public RolService(IRolRepository rolRepository)
		{
			_rolRepository = rolRepository;
		}

		public List<RolSelectDto> Listar()
		{
			return _rolRepository.Listar();
		}

		public RolSelectDto? Obtener(int id)
		{
			return _rolRepository.Obtener(id);
		}

		public int Insertar(RolInsertDto modelo)
		{
			return _rolRepository.Insertar(modelo);
		}

		public void Actualizar(RolUpdateDto modelo)
		{
			_rolRepository.Actualizar(modelo);
		}

		public void Eliminar(int id)
		{
			_rolRepository.Eliminar(id);
		}
	}
}
