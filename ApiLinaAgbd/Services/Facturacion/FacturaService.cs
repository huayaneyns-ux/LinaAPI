using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.Factura;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class FacturaService
	{
		private readonly FacturaUblBuilder _builder;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly FacturacionSettings _settings;

		public FacturaService(
			FacturaUblBuilder builder,
			FacturacionSunatService facturacionSunatService,
			IOptions<FacturacionSettings> options)
		{
			_builder = builder;
			_facturacionSunatService = facturacionSunatService;
			_settings = options.Value;
		}

		public Task<FacturacionEnvioResultado> Enviar(FacturaRequestDto request)
		{
			if (string.IsNullOrWhiteSpace(_settings.Emisor?.Ruc))
			{
				return Task.FromResult(new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status500InternalServerError,
					Mensaje = "Falta FacturacionSettings:Emisor:Ruc para armar el fileName del comprobante."
				});
			}

			var documentBody = _builder.Build(request);
			var fileName =
				$"{_settings.Emisor.Ruc}-{FacturaUblBuilder.TipoDocumentoFactura}-{request.Serie}-{request.Correlativo}";

			return _facturacionSunatService.EnviarDocumento(fileName, documentBody);
		}
	}
}
