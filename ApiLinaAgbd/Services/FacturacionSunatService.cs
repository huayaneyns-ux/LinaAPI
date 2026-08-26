using System.Net.Http.Json;
using System.Text.Json;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services
{
	public class FacturacionSunatService
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
		};

		private readonly HttpClient _httpClient;
		private readonly FacturacionSettings _settings;
		private readonly ILogger<FacturacionSunatService> _logger;

		public FacturacionSunatService(
			HttpClient httpClient,
			IOptions<FacturacionSettings> options,
			ILogger<FacturacionSunatService> logger)
		{
			_httpClient = httpClient;
			_settings = options.Value;
			_logger = logger;
		}

		public async Task<FacturacionEnvioResultado> EnviarDocumento(string fileName, object documentBody)
		{
			if (string.IsNullOrWhiteSpace(_settings.PersonaId) ||
				string.IsNullOrWhiteSpace(_settings.PersonaToken))
			{
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status500InternalServerError,
					Mensaje = "Falta PersonaId o PersonaToken en FacturacionSettings.",
					FileName = fileName
				};
			}

			var payload = new SendBillRequest
			{
				PersonaId = _settings.PersonaId,
				PersonaToken = _settings.PersonaToken,
				FileName = fileName,
				DocumentBody = documentBody
			};

			var path = string.IsNullOrWhiteSpace(_settings.SendBillPath)
				? "personas/v1/sendBill"
				: _settings.SendBillPath.TrimStart('/');

			try
			{
				using var response = await _httpClient.PostAsJsonAsync(path, payload, JsonOptions);
				var cuerpo = await response.Content.ReadAsStringAsync();

				return new FacturacionEnvioResultado
				{
					Exitoso = response.IsSuccessStatusCode,
					StatusCode = (int)response.StatusCode,
					Mensaje = response.IsSuccessStatusCode
						? "Documento enviado a la API de facturación."
						: "La API de facturación devolvió un error HTTP.",
					FileName = fileName,
					DetalleError = response.IsSuccessStatusCode ? null : Truncar(cuerpo),
					RespuestaApi = ParsearCuerpo(cuerpo)
				};
			}
			catch (TaskCanceledException ex)
			{
				_logger.LogError(ex, "Timeout al enviar {FileName} a APISUNAT.", fileName);

				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status504GatewayTimeout,
					Mensaje = "Timeout al comunicar con la API de facturación.",
					FileName = fileName,
					DetalleError = ex.Message
				};
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Error de comunicación al enviar {FileName} a APISUNAT.", fileName);

				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status502BadGateway,
					Mensaje = "Error de comunicación con la API de facturación.",
					FileName = fileName,
					DetalleError = ex.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error inesperado al enviar {FileName} a APISUNAT.", fileName);

				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status500InternalServerError,
					Mensaje = "Error inesperado al enviar el documento de facturación.",
					FileName = fileName,
					DetalleError = ex.Message
				};
			}
		}

		private static object? ParsearCuerpo(string cuerpo)
		{
			if (string.IsNullOrWhiteSpace(cuerpo))
			{
				return null;
			}

			try
			{
				return JsonSerializer.Deserialize<JsonElement>(cuerpo);
			}
			catch (JsonException)
			{
				return cuerpo;
			}
		}

		private static string Truncar(string texto, int max = 4000)
		{
			if (string.IsNullOrEmpty(texto) || texto.Length <= max)
			{
				return texto;
			}

			return texto[..max];
		}
	}
}
