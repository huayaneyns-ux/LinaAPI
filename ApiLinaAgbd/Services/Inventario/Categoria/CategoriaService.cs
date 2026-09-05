using ApiLinaAgbd.Models.Inventario.Categorias;
using ApiLinaAgbd.Repositories.Inventario.Categoria;

namespace ApiLinaAgbd.Services.Inventario.Categoria
{
	public class CategoriaService : ICategoriaService
	{
		private readonly ICategoriaRepository _categoriaRepository;

		public CategoriaService(ICategoriaRepository categoriaRepository)
		{
			_categoriaRepository = categoriaRepository;
		}

		public List<CategoriaSelectDto> Listar()
		{
			return _categoriaRepository.Listar();
		}

		public void Insertar(CategoriaInsertDto categoria)
		{
			_categoriaRepository.Insertar(categoria);
		}

		public void Actualizar(CategoriaUpdateDto categoria)
		{
			_categoriaRepository.Actualizar(categoria);
		}

		public void Eliminar(int id)
		{
			_categoriaRepository.Eliminar(id);
		}

		public CategoriaObtenerIDDto? ObtenerPorId(int id)
		{
			return _categoriaRepository.ObtenerPorId(id);
		}
	}
}
