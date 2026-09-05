using ApiLinaAgbd.Models.MetodoPago;
using ApiLinaAgbd.Repositories.MetodoPago;

namespace ApiLinaAgbd.Services.MetodoPago
{
	public class MetodoPagoService : IMetodoPagoService
	{
		private readonly IMetodoPagoRepository _metodoPagoRepository;

		public MetodoPagoService(IMetodoPagoRepository metodoPagoRepository)
		{
			_metodoPagoRepository = metodoPagoRepository;
		}

		public List<MetodoPagoSelectDto> Listar()
		{
			return _metodoPagoRepository.Listar();
		}
	}
}
