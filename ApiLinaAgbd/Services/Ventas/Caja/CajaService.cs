using ApiLinaAgbd.Models.Ventas.Caja;
using ApiLinaAgbd.Repositories.Ventas.Caja;
using ApiLinaAgbd.Services.Persona;

namespace ApiLinaAgbd.Services.Ventas.Caja
{
	public class CajaService : ICajaService
	{
		private readonly ICajaRepository _cajaRepository;
		private readonly IApiPeruService _apiPeruService;

		public CajaService(ICajaRepository cajaRepository, IApiPeruService apiPeruService)
		{
			_cajaRepository = cajaRepository;
			_apiPeruService = apiPeruService;
		}

		public int RegistrarVenta(CajaVentaInsertDto venta)
		{
			var tipoComprobante = (venta.TipoComprobante ?? "BOLETA").Trim().ToUpperInvariant();
			var subtotal = venta.Detalle?.Sum(item => item.Cantidad * item.PrecioUnitario) ?? 0m;
			var total = subtotal * 1.18m;
			if (!venta.IdCliente.HasValue &&
				(tipoComprobante != "SIN_COMPROBANTE" || total > 5m))
				throw new ArgumentException("Una venta sin comprobante solo se permite hasta S/ 5.");

			return _cajaRepository.RegistrarVenta(venta);
		}

		public CajaClienteDto? BuscarCliente(string dni)
		{
			return _cajaRepository.BuscarCliente(dni);
		}

		public async Task<CajaClienteDto?> BuscarClientePorDocumentoAsync(string tipoDocumento, string numero)
		{
			tipoDocumento = (tipoDocumento ?? string.Empty).Trim().ToUpperInvariant();
			numero = (numero ?? string.Empty).Trim();
			if (tipoDocumento is not ("DNI" or "RUC") || string.IsNullOrWhiteSpace(numero))
				return null;

			var existente = _cajaRepository.BuscarClientePorDocumento(tipoDocumento, numero);
			if (existente is not null)
				return existente;

			var persona = await _apiPeruService.ConsultarYRegistrarPersonaAsync(tipoDocumento, numero);
			if (!persona.Success || string.IsNullOrWhiteSpace(persona.Nombre))
				return null;

			_cajaRepository.CrearOReutilizarCliente(new CajaClienteInsertDto
			{
				TipoDocumento = tipoDocumento,
				Documento = persona.Numero ?? numero,
				DNI = tipoDocumento == "DNI" ? persona.Numero ?? numero : string.Empty,
				NombreApellido = persona.Nombre,
				Direccion = persona.Direccion ?? string.Empty,
				Ubigeo = persona.Ubigeo ?? string.Empty
			});

			return _cajaRepository.BuscarClientePorDocumento(
				tipoDocumento,
				persona.Numero ?? numero);
		}

		public async Task<CajaClienteDto?> CrearClienteAsync(CajaClienteInsertDto cliente)
		{
			var tipo = string.IsNullOrWhiteSpace(cliente.TipoDocumento)
				? (cliente.DNI?.Length == 11 ? "RUC" : "DNI")
				: cliente.TipoDocumento.Trim().ToUpperInvariant();
			var numero = string.IsNullOrWhiteSpace(cliente.Documento) ? cliente.DNI : cliente.Documento;
			if (tipo is not ("DNI" or "RUC") ||
				(tipo == "DNI" && numero.Length != 8) ||
				(tipo == "RUC" && numero.Length != 11))
				return null;

			var persona = await _apiPeruService.ConsultarYRegistrarPersonaAsync(tipo, numero);
			if (!persona.Success || string.IsNullOrWhiteSpace(persona.Nombre))
				return null;

			_cajaRepository.CrearOReutilizarCliente(new CajaClienteInsertDto
			{
				TipoDocumento = tipo,
				Documento = persona.Numero ?? numero,
				DNI = tipo == "DNI" ? persona.Numero ?? numero : string.Empty,
				NombreApellido = persona.Nombre,
				Direccion = persona.Direccion ?? string.Empty,
				Ubigeo = persona.Ubigeo ?? string.Empty,
				Telefono = cliente.Telefono ?? string.Empty,
				Correo = cliente.Correo ?? string.Empty
			});
			return _cajaRepository.BuscarClientePorDocumento(tipo, persona.Numero ?? numero);
		}


		public void RegistrarPago(int id, CajaPagoInsertDto pago)
		{
			_cajaRepository.RegistrarPago(id, pago);
		}
	}
}
