using ApiLinaAgbd.Models.MetodoPago;

namespace ApiLinaAgbd.Services.MetodoPago
{
	public interface IMetodoPagoService
	{
		List<MetodoPagoSelectDto> Listar();
	}
}
