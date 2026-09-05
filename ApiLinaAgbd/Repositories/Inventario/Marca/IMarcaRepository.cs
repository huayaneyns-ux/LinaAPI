using ApiLinaAgbd.Models.Inventario.Marca;

namespace ApiLinaAgbd.Repositories.Inventario.Marca
{
	public interface IMarcaRepository
	{
		List<MarcaSelectDto> Listar();
		void Insertar(MarcaInsertDto marca);
		void Actualizar(MarcaUpdateDto marca);
		void Eliminar(int id);
		MarcaObtenerIDDto? ObtenerPorId(int id);
	}
}
