using ApiLinaAgbd.Models.Compras.Compra;
using ApiLinaAgbd.Repositories.Compras.Compra;

namespace ApiLinaAgbd.Services.Compras.Compra
{
	public class CompraService : ICompraService
	{
		private readonly ICompraRepository _compraRepository;

		public CompraService(ICompraRepository compraRepository)
		{
			_compraRepository = compraRepository;
		}

		public CompraRegistrarResult RegistrarCompleta(CompraCompletaInsertDto modelo)
		{
			try
			{
				int idCompra = _compraRepository.RegistrarCompleta(modelo);

				return new CompraRegistrarResult(
					true,
					"Compra registrada correctamente.",
					idCompra
				);
			}
			catch (Exception ex)
			{
				return new CompraRegistrarResult(false, ex.Message);
			}
		}

		public List<CompraDetalleSelectDto> ObtenerDetalle(int id)
		{
			return _compraRepository.ObtenerDetalle(id);
		}

		public List<CompraListaDto> Listar()
		{
			return _compraRepository.Listar();
		}
	}
}
