using ApiLinaAgbd.Models.Inventario.Marca;

namespace ApiLinaAgbd.Services.Inventario.Marca
{
	public interface IMarcaService
	{
		List<MarcaSelectDto> Listar();
		void Insertar(MarcaInsertDto marca);
		void Actualizar(MarcaUpdateDto marca);
		void Eliminar(int id);
		MarcaObtenerIDDto? ObtenerPorId(int id);
	}
}
