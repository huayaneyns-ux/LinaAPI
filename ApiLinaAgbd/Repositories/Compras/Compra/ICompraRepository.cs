using ApiLinaAgbd.Models.Compras.Compra;

namespace ApiLinaAgbd.Repositories.Compras.Compra
{
	public interface ICompraRepository
	{
		int RegistrarCompleta(CompraCompletaInsertDto modelo);
		List<CompraDetalleSelectDto> ObtenerDetalle(int id);
		List<CompraListaDto> Listar();
	}
}
