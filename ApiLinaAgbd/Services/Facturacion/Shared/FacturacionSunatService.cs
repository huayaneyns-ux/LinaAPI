using System.Net.Http.Json;
using System.Text.Json;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Models.Facturacion.Ubl;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Facturacion.Shared
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
				_logger.LogInformation(
					"Enviando {FileName} a APISUNAT con documentBody: {DocumentBody}",
					fileName,
					JsonSerializer.Serialize(documentBody, JsonOptions));

				using var response = await _httpClient.PostAsJsonAsync(path, payload, JsonOptions);
				var cuerpo = await response.Content.ReadAsStringAsync();
				var respuestaApi = ParsearCuerpo(cuerpo);
				var estadoSunat = ObtenerValor(respuestaApi, "status");
				var codigoRespuestaSunat =
					ObtenerValor(respuestaApi, "responseCode") ??
					ObtenerValor(respuestaApi, "code");
				var mensajeSunat = ObtenerMensajeDocumento(respuestaApi);
				var documentId =
					ObtenerValor(respuestaApi, "documentId") ??
					ObtenerValor(respuestaApi, "id");
				var xmlUrl =
					ObtenerValor(respuestaApi, "xmlUrl") ??
					ObtenerValorAnidado(respuestaApi, "document", "xmlUrl");
				var pdfUrl =
					ObtenerValor(respuestaApi, "pdfUrl") ??
					ObtenerValorAnidado(respuestaApi, "document", "pdfUrl");
				var cdrUrl =
					ObtenerValor(respuestaApi, "cdrUrl") ??
					ObtenerValorAnidado(respuestaApi, "document", "cdrUrl");

				return new FacturacionEnvioResultado
				{
					Exitoso = response.IsSuccessStatusCode,
					StatusCode = (int)response.StatusCode,
					Mensaje = response.IsSuccessStatusCode
						? "Documento enviado a la API de facturación."
						: "La API de facturación devolvió un error HTTP.",
					FileName = fileName,
					DetalleError = response.IsSuccessStatusCode ? null : Truncar(cuerpo),
					RespuestaApi = respuestaApi,
					EstadoSunat = estadoSunat,
					CodigoRespuestaSunat = codigoRespuestaSunat,
					MensajeSunat = mensajeSunat,
					DocumentId = documentId,
					XmlUrl = xmlUrl,
					PdfUrl = pdfUrl,
					CdrUrl = cdrUrl
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

		public async Task<FacturacionEnvioResultado> ObtenerDocumentoPorId(string documentId)
		{
			if (string.IsNullOrWhiteSpace(documentId))
			{
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status400BadRequest,
					Mensaje = "El documentId es obligatorio."
				};
			}

			try
			{
				using var response = await _httpClient.GetAsync($"documents/{documentId}/getById");
				var cuerpo = await response.Content.ReadAsStringAsync();
				var respuestaApi = ParsearCuerpo(cuerpo);
				var fileName = ObtenerValor(respuestaApi, "fileName");
				var xmlUrl = ObtenerValor(respuestaApi, "xml");
				var cdrUrl = ObtenerValor(respuestaApi, "cdr");
				var estadoSunat = ObtenerValor(respuestaApi, "status");
				var mensajeSunat = ObtenerMensajeDocumento(respuestaApi);

				return new FacturacionEnvioResultado
				{
					Exitoso = response.IsSuccessStatusCode,
					StatusCode = (int)response.StatusCode,
					Mensaje = response.IsSuccessStatusCode
						? "Documento consultado en APISUNAT."
						: "La API de facturación devolvió un error HTTP al consultar el documento.",
					FileName = fileName,
					DetalleError = response.IsSuccessStatusCode ? null : Truncar(cuerpo),
					RespuestaApi = respuestaApi,
					EstadoSunat = estadoSunat,
					MensajeSunat = mensajeSunat,
					DocumentId = documentId,
					XmlUrl = xmlUrl,
					CdrUrl = cdrUrl
				};
			}
			catch (TaskCanceledException ex)
			{
				_logger.LogError(ex, "Timeout al consultar documento {DocumentId} en APISUNAT.", documentId);
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status504GatewayTimeout,
					Mensaje = "Timeout al consultar el documento en la API de facturación.",
					DocumentId = documentId,
					DetalleError = ex.Message
				};
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Error de comunicación al consultar documento {DocumentId} en APISUNAT.", documentId);
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status502BadGateway,
					Mensaje = "Error de comunicación con la API de facturación al consultar el documento.",
					DocumentId = documentId,
					DetalleError = ex.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error inesperado al consultar documento {DocumentId} en APISUNAT.", documentId);
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status500InternalServerError,
					Mensaje = "Error inesperado al consultar el documento.",
					DocumentId = documentId,
					DetalleError = ex.Message
				};
			}
		}

		public async Task<(byte[] Content, string ContentType, string FileName)> DescargarPdf(string documentId, string format, string fileName)
		{
			using var response = await _httpClient.GetAsync($"documents/{documentId}/getPDF/{format}/{fileName}.pdf");
			if (!response.IsSuccessStatusCode)
			{
				var cuerpo = await response.Content.ReadAsStringAsync();
				throw new InvalidOperationException(
					$"No se pudo descargar el PDF desde APISUNAT. HTTP {(int)response.StatusCode}: {Truncar(cuerpo, 500)}");
			}

			var contenido = await response.Content.ReadAsByteArrayAsync();
			var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
			return (contenido, contentType, $"{fileName}.pdf");
		}

		public async Task<(byte[] Content, string ContentType)> DescargarContenidoAsync(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new InvalidOperationException("La URL del archivo está vacía.");
			}

			using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
			if (!response.IsSuccessStatusCode)
			{
				var cuerpo = await response.Content.ReadAsStringAsync();
				throw new InvalidOperationException(
					$"No se pudo descargar el archivo desde APISUNAT. HTTP {(int)response.StatusCode}: {Truncar(cuerpo, 500)}");
			}

			var contenido = await response.Content.ReadAsByteArrayAsync();
			var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
			return (contenido, contentType);
		}

		public async Task<FacturacionEnvioResultado> AnularDocumento(string documentId, string reason)
		{
			if (string.IsNullOrWhiteSpace(_settings.PersonaId) ||
				string.IsNullOrWhiteSpace(_settings.PersonaToken))
			{
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status500InternalServerError,
					Mensaje = "Falta PersonaId o PersonaToken en FacturacionSettings.",
					DocumentId = documentId
				};
			}

			var payload = new
			{
				personaId = _settings.PersonaId,
				personaToken = _settings.PersonaToken,
				documentId,
				reason
			};

			try
			{
				using var response = await _httpClient.PostAsJsonAsync("personas/v1/voidBill", payload, JsonOptions);
				var cuerpo = await response.Content.ReadAsStringAsync();
				var respuestaApi = ParsearCuerpo(cuerpo);
				var estadoSunat = ObtenerValor(respuestaApi, "status");
				if (response.IsSuccessStatusCode)
				{
					var estadoNormalizado = (estadoSunat ?? string.Empty).Trim().ToUpperInvariant();
					estadoSunat = estadoNormalizado switch
					{
						"RECHAZADO" => "RECHAZADO",
						"EXCEPCION" => "EXCEPCION",
						"OBSERVADO" => "OBSERVADO",
						"ACEPTADO" => "ACEPTADO",
						"PENDING" => "ACEPTADO",
						"SUCCESS" => "ACEPTADO",
						"ENVIADO" => "PENDIENTE",
						_ => "ACEPTADO"
					};
				}

				return new FacturacionEnvioResultado
				{
					Exitoso = response.IsSuccessStatusCode,
					StatusCode = (int)response.StatusCode,
					Mensaje = response.IsSuccessStatusCode
						? "Documento enviado a anulación en APISUNAT."
						: "La API de facturación devolvió un error HTTP al anular el documento.",
					DocumentId = documentId,
					RespuestaApi = respuestaApi,
					EstadoSunat = estadoSunat,
					MensajeSunat = ObtenerMensajeDocumento(respuestaApi)
				};
			}
			catch (TaskCanceledException ex)
			{
				_logger.LogError(ex, "Timeout al anular documento {DocumentId} en APISUNAT.", documentId);
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status504GatewayTimeout,
					Mensaje = "Timeout al anular el documento en la API de facturación.",
					DocumentId = documentId,
					DetalleError = ex.Message
				};
			}
			catch (HttpRequestException ex)
			{
				_logger.LogError(ex, "Error de comunicación al anular documento {DocumentId} en APISUNAT.", documentId);
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status502BadGateway,
					Mensaje = "Error de comunicación con la API de facturación al anular el documento.",
					DocumentId = documentId,
					DetalleError = ex.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error inesperado al anular documento {DocumentId} en APISUNAT.", documentId);
				return new FacturacionEnvioResultado
				{
					Exitoso = false,
					StatusCode = StatusCodes.Status500InternalServerError,
					Mensaje = "Error inesperado al anular el documento.",
					DocumentId = documentId,
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

		private static string? ObtenerValor(object? origen, string propiedad)
		{
			if (origen is not JsonElement element || element.ValueKind != JsonValueKind.Object)
			{
				return null;
			}

			if (!element.TryGetProperty(propiedad, out var value))
			{
				return null;
			}

			return value.ValueKind switch
			{
				JsonValueKind.String => value.GetString(),
				JsonValueKind.Number => value.ToString(),
				JsonValueKind.True => bool.TrueString,
				JsonValueKind.False => bool.FalseString,
				_ => value.ToString()
			};
		}

		private static string? ObtenerValorAnidado(object? origen, string objeto, string propiedad)
		{
			if (origen is not JsonElement element || element.ValueKind != JsonValueKind.Object)
			{
				return null;
			}

			if (!element.TryGetProperty(objeto, out var nested) || nested.ValueKind != JsonValueKind.Object)
			{
				return null;
			}

			if (!nested.TryGetProperty(propiedad, out var value))
			{
				return null;
			}

			return value.ValueKind switch
			{
				JsonValueKind.String => value.GetString(),
				JsonValueKind.Number => value.ToString(),
				JsonValueKind.True => bool.TrueString,
				JsonValueKind.False => bool.FalseString,
				_ => value.ToString()
			};
		}

		private static string? ObtenerMensajeDocumento(object? respuestaApi)
		{
			if (respuestaApi is not JsonElement element || element.ValueKind != JsonValueKind.Object)
			{
				return null;
			}

			if (element.TryGetProperty("notes", out var notes) && notes.ValueKind == JsonValueKind.Array)
			{
				var valores = notes.EnumerateArray()
					.Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
					.Where(x => !string.IsNullOrWhiteSpace(x));
				var mensaje = string.Join(" | ", valores!);
				if (!string.IsNullOrWhiteSpace(mensaje))
				{
					return mensaje;
				}
			}

			if (element.TryGetProperty("faults", out var faults) && faults.ValueKind == JsonValueKind.Array)
			{
				var valores = faults.EnumerateArray()
					.Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
					.Where(x => !string.IsNullOrWhiteSpace(x));
				var mensaje = string.Join(" | ", valores!);
				if (!string.IsNullOrWhiteSpace(mensaje))
				{
					return mensaje;
				}
			}

			return ObtenerValor(respuestaApi, "message") ?? ObtenerValor(respuestaApi, "description");
		}
	}
}
