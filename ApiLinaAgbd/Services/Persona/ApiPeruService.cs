using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ApiLinaAgbd.Models.ApiPeru;
using ApiLinaAgbd.Models.Persona;
using ApiLinaAgbd.Repositories.Persona;
using Microsoft.Extensions.Options;

namespace ApiLinaAgbd.Services.Persona
{
	public class ApiPeruService : IApiPeruService
	{
		private readonly IPersonaRepository _personaRepository;
		private readonly HttpClient _httpClient;
		private readonly ApiPeruSettings _settings;
		private readonly ILogger<ApiPeruService> _logger;

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNameCaseInsensitive = true
		};

		public ApiPeruService(
			IPersonaRepository personaRepository,
			HttpClient httpClient,
			IOptions<ApiPeruSettings> options,
			ILogger<ApiPeruService> logger)
		{
			_personaRepository = personaRepository;
			_httpClient = httpClient;
			_settings = options.Value;
			_logger = logger;
		}

		public async Task<PersonaResponseDto> ConsultarYRegistrarPersonaAsync(string tipoDocumento, string numero)
		{
			if (string.IsNullOrWhiteSpace(tipoDocumento) || string.IsNullOrWhiteSpace(numero))
			{
				return new PersonaResponseDto
				{
					Success = false,
					Mensaje = "El tipo de documento y el número son requeridos."
				};
			}

			tipoDocumento = tipoDocumento.Trim().ToUpperInvariant();
			numero = numero.Trim();

			if (tipoDocumento != "DNI" && tipoDocumento != "RUC")
			{
				return new PersonaResponseDto
				{
					Success = false,
					Mensaje = "Tipo de documento no válido. Debe ser 'DNI' o 'RUC'."
				};
			}

			// Primero se consulta la base de datos. API Perú solo se usa cuando el documento no existe.
			var personaBd = _personaRepository.Buscar(tipoDocumento, numero);
			if (personaBd != null)
			{
				return new PersonaResponseDto
				{
					Success = true,
					Mensaje = "Persona encontrada en base de datos.",
					Numero = personaBd.Numero,
					Nombre = personaBd.Nombre,
					Direccion = personaBd.Direccion,
					Origen = "BD"
				};
			}

			var personaApi = await ConsultarApiPeruAsync(tipoDocumento, numero);
			if (personaApi != null && !string.IsNullOrWhiteSpace(personaApi.Nombre))
			{
				_personaRepository.Registrar(
					tipoDocumento,
					personaApi.Numero ?? numero,
					personaApi.Nombre,
					personaApi.Direccion,
					personaApi.Ubigeo);

				return new PersonaResponseDto
				{
					Success = true,
					Mensaje = "Persona obtenida de ApiPeru y registrada en base de datos.",
					Numero = personaApi.Numero ?? numero,
					Nombre = personaApi.Nombre,
					Direccion = personaApi.Direccion,
					Ubigeo = personaApi.Ubigeo,
					Origen = "API"
				};
			}

			return new PersonaResponseDto
			{
				Success = false,
				Mensaje = $"No se encontró información en ApiPeru para {tipoDocumento}: {numero}."
			};
		}

		private async Task<PersonaData?> ConsultarApiPeruAsync(string tipoDocumento, string numero)
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Post, tipoDocumento == "DNI" ? "dni" : "ruc");

				if (!string.IsNullOrWhiteSpace(_settings.Token))
				{
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
				}

				if (tipoDocumento == "DNI")
				{
					request.Content = JsonContent.Create(new { dni = numero });
					using var response = await _httpClient.SendAsync(request);

					if (!response.IsSuccessStatusCode)
					{
						_logger.LogWarning("ApiPeru DNI retornó StatusCode {StatusCode}", response.StatusCode);
						return null;
					}

					var result = await response.Content.ReadFromJsonAsync<ApiPeruDniResponse>(JsonOptions);
					if (result?.Success == true && result.Data != null)
					{
						string nombre = !string.IsNullOrWhiteSpace(result.Data.NombreCompleto)
							? result.Data.NombreCompleto.Trim()
							: $"{result.Data.Nombres} {result.Data.ApellidoPaterno} {result.Data.ApellidoMaterno}".Trim();

						string num = !string.IsNullOrWhiteSpace(result.Data.Numero)
							? result.Data.Numero.Trim()
							: numero;

						return new PersonaData
						{
							Numero = num,
							Nombre = nombre,
							Direccion = string.IsNullOrWhiteSpace(result.Data.DireccionCompleta)
								? result.Data.Direccion
								: result.Data.DireccionCompleta,
							Ubigeo = NormalizarUbigeo(result.Data.Ubigeo, result.Data.UbigeoSunat)
						};
					}
				}
				else if (tipoDocumento == "RUC")
				{
					request.Content = JsonContent.Create(new { ruc = numero });
					using var response = await _httpClient.SendAsync(request);

					if (!response.IsSuccessStatusCode)
					{
						_logger.LogWarning("ApiPeru RUC retornó StatusCode {StatusCode}", response.StatusCode);
						return null;
					}

					var result = await response.Content.ReadFromJsonAsync<ApiPeruRucResponse>(JsonOptions);
					if (result?.Success == true && result.Data != null)
					{
						string nombre = result.Data.NombreORazonSocial?.Trim() ?? "";
						string num = !string.IsNullOrWhiteSpace(result.Data.Ruc)
							? result.Data.Ruc.Trim()
							: numero;

						return new PersonaData
						{
							Numero = num,
							Nombre = nombre,
							Direccion = string.IsNullOrWhiteSpace(result.Data.DireccionCompleta)
								? result.Data.Direccion
								: result.Data.DireccionCompleta,
							Ubigeo = NormalizarUbigeo(result.Data.Ubigeo, result.Data.UbigeoSunat)
						};
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al consultar ApiPeru con tipo {Tipo} y número {Numero}", tipoDocumento, numero);
			}

			return null;
		}

		private static string? NormalizarUbigeo(string[]? ubigeo, string? ubigeoSunat)
		{
			var completo = ubigeo?.LastOrDefault(x => !string.IsNullOrWhiteSpace(x) && x.Trim().Length == 6);
			return string.IsNullOrWhiteSpace(completo) ? ubigeoSunat?.Trim() : completo.Trim();
		}
	}
}
