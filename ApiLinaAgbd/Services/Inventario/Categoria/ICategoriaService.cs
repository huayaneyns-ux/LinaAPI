using ApiLinaAgbd.Models.Inventario.Categorias;

namespace ApiLinaAgbd.Services.Inventario.Categoria
{
	public interface ICategoriaService
	{
		List<CategoriaSelectDto> Listar();
		void Insertar(CategoriaInsertDto categoria);
		void Actualizar(CategoriaUpdateDto categoria);
		void Eliminar(int id);
		CategoriaObtenerIDDto? ObtenerPorId(int id);
	}
}
