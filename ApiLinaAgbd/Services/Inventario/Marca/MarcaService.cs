using ApiLinaAgbd.Models.Inventario.Marca;
using ApiLinaAgbd.Repositories.Inventario.Marca;

namespace ApiLinaAgbd.Services.Inventario.Marca
{
	public class MarcaService : IMarcaService
	{
		private readonly IMarcaRepository _marcaRepository;

		public MarcaService(IMarcaRepository marcaRepository)
		{
			_marcaRepository = marcaRepository;
		}

		public List<MarcaSelectDto> Listar()
		{
			return _marcaRepository.Listar();
		}

		public void Insertar(MarcaInsertDto marca)
		{
			_marcaRepository.Insertar(marca);
		}

		public void Actualizar(MarcaUpdateDto marca)
		{
			_marcaRepository.Actualizar(marca);
		}

		public void Eliminar(int id)
		{
			_marcaRepository.Eliminar(id);
		}

		public MarcaObtenerIDDto? ObtenerPorId(int id)
		{
			return _marcaRepository.ObtenerPorId(id);
		}
	}
}
