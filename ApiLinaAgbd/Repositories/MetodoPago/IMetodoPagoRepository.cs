using ApiLinaAgbd.Models.MetodoPago;

namespace ApiLinaAgbd.Repositories.MetodoPago
{
	public interface IMetodoPagoRepository
	{
		List<MetodoPagoSelectDto> Listar();
	}
}
