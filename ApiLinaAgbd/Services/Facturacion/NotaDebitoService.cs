using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.NotaDebito;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion
{
	public class NotaDebitoService
	{
		private readonly NotaDebitoUblBuilder _builder;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly FacturacionSettings _settings;

		public NotaDebitoService(
			NotaDebitoUblBuilder builder,
			FacturacionSunatService facturacionSunatService,
			IOptions<FacturacionSettings> options)
		{
			_builder = builder;
			_facturacionSunatService = facturacionSunatService;
			_settings = options.Value;
		}

		public Task<FacturacionEnvioResultado> Enviar(NotaDebitoRequestDto request)
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
				$"{_settings.Emisor.Ruc}-{NotaDebitoUblBuilder.TipoDocumentoNotaDebito}-{request.Serie}-{request.Correlativo}";

			return _facturacionSunatService.EnviarDocumento(fileName, documentBody);
		}
	}
}
