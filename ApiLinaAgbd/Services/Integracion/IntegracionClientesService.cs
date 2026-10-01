using ApiLinaAgbd.Models.Integracion;
using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Repositories.Seguridad.Usuario;
using ApiLinaAgbd.Repositories.Integracion;

namespace ApiLinaAgbd.Services.Integracion;

public class IntegracionClientesService : IIntegracionClientesService
{
	private const int RolCliente = 1;
	private readonly IUsuarioRepository _usuarioRepository;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<IntegracionClientesService> _logger;
	private readonly IIntegracionRepository _integracionRepository;

	public IntegracionClientesService(
		IUsuarioRepository usuarioRepository,
		IHttpClientFactory httpClientFactory,
		ILogger<IntegracionClientesService> logger,
		IIntegracionRepository integracionRepository)
	{
		_usuarioRepository = usuarioRepository;
		_httpClientFactory = httpClientFactory;
		_logger = logger;
		_integracionRepository = integracionRepository;
	}

	public List<ClienteIntegracionDto> ListarClientes()
	{
		return _usuarioRepository.Listar()
			.Where(usuario => usuario.idRol == RolCliente)
			.Select(Mapear)
			.ToList();
	}

	public (ClienteIntegracionDto Cliente, bool YaExistia, string? CampoDuplicado) RegistrarClienteExterno(ClienteWebhookDto cliente, string integrationKey)
	{
		var documento = cliente.documento?.Trim();
		// El webhook externo solo maneja el número; el tipo se deduce internamente
		// para consultar la tabla documento.
		var tipoDocumento = documento?.Length == 11 ? "RUC" : "DNI";
		var existente = string.IsNullOrWhiteSpace(documento)
			? null
			: _usuarioRepository.ObtenerPorDocumento(tipoDocumento, documento);

		if (existente is not null)
		{
			return (Mapear(existente), true, "documento");
		}

		var correo = cliente.email?.Trim();
		var existentePorCorreo = string.IsNullOrWhiteSpace(correo)
			? null
			: _usuarioRepository.ObtenerPorCorreo(correo);
		if (existentePorCorreo is not null)
		{
			return (Mapear(existentePorCorreo), true, "correo");
		}

		var idIntegracion = _integracionRepository.ObtenerIdPorApiKey(integrationKey)
			?? throw new UnauthorizedAccessException("La clave de integración no corresponde a una empresa activa.");

		var id = _usuarioRepository.Guardar(new UsuarioInsertUpdateDto
		{
			nombreApellido = cliente.nombre.Trim(),
			dni = string.IsNullOrWhiteSpace(documento) ? $"EXT-{Guid.NewGuid():N}" : documento,
			tipoDocumento = tipoDocumento,
			sexo = string.Empty,
			telefono = cliente.telefono,
			correo = cliente.email ?? string.Empty,
			// El cliente externo no recibe credenciales por este webhook.
			contrasena = Guid.NewGuid().ToString("N"),
			idRol = RolCliente,
			estado = true,
			idIntegracionSistema = idIntegracion
		});

		var registrado = _usuarioRepository.Obtener(id)
			?? throw new InvalidOperationException("No se pudo recuperar el cliente recién registrado.");
		return (Mapear(registrado), false, null);
	}

	public async Task NotificarClienteNuevoAsync(ClienteIntegracionDto cliente)
	{
		var empresas = _integracionRepository.ListarConfiguraciones()
			.Where(empresa => empresa.Estado &&
				!string.IsNullOrWhiteSpace(empresa.DominioEndpoint) &&
				!string.IsNullOrWhiteSpace(empresa.ApiKeyExterna))
			.ToList();
		if (empresas.Count == 0)
		{
			_logger.LogWarning("No hay empresas externas configuradas para notificar el cliente {Documento}.", cliente.documento);
			return;
		}

		// Contrato compatible con el manual de Acabados J&S.
		// El tipo de documento se conserva internamente en Lina; ellos reciben documento.
		var payload = new
		{
			nombre = cliente.nombre,
			documento = cliente.documento,
			telefono = cliente.telefono,
			email = cliente.email,
			direccion = (string?)null,
			origen = "lina",
		};

		foreach (var empresa in empresas)
		{
			var inicio = DateTime.UtcNow;
			long? auditoriaId = null;
			try
			{
				auditoriaId = _integracionRepository.IniciarAuditoria(
					empresa.ApiKey, "WEBHOOK_CLIENTE_NUEVO", inicio, null);
				using var request = new HttpRequestMessage(HttpMethod.Post,
					ConstruirUrl(empresa.DominioEndpoint, "/api/v1/webhooks/cliente-externo"));
				request.Headers.Add("X-API-Key", empresa.ApiKeyExterna);
				request.Content = JsonContent.Create(payload);

				var client = _httpClientFactory.CreateClient("IntegracionClientes");
				using var response = await client.SendAsync(request);
				var responseBody = await response.Content.ReadAsStringAsync();
				if (!response.IsSuccessStatusCode)
				{
					_integracionRepository.FinalizarAuditoria(auditoriaId.Value, DateTime.UtcNow,
						(long)(DateTime.UtcNow - inicio).TotalMilliseconds, "ERROR", 0,
						$"HTTP {(int)response.StatusCode}: {responseBody[..Math.Min(responseBody.Length, 900)]}");
					_logger.LogWarning("{Empresa} respondió {StatusCode} al registrar el cliente {Documento}. Respuesta: {Respuesta}", empresa.NombreEmpresa, response.StatusCode, cliente.documento, responseBody);
					continue;
				}

				_integracionRepository.FinalizarAuditoria(auditoriaId.Value, DateTime.UtcNow,
					(long)(DateTime.UtcNow - inicio).TotalMilliseconds, "EXITOSO", 1, null);
			}
			catch (Exception exception)
			{
				if (auditoriaId.HasValue)
					_integracionRepository.FinalizarAuditoria(auditoriaId.Value, DateTime.UtcNow,
						(long)(DateTime.UtcNow - inicio).TotalMilliseconds, "ERROR", 0,
						exception.Message[..Math.Min(exception.Message.Length, 1000)]);
				_logger.LogError(exception, "No se pudo notificar el cliente {Documento} a {Empresa}.", cliente.documento, empresa.NombreEmpresa);
			}
		}
	}

	private static ClienteIntegracionDto Mapear(UsuarioSelectDto usuario) => new()
	{
		id = usuario.id,
		nombre = usuario.nombreApellido,
		documento = usuario.dni,
		telefono = usuario.telefono,
		email = usuario.correo,
	};

	private static string ConstruirUrl(string dominio, string ruta)
	{
		var baseUrl = dominio.TrimEnd('/');
		if (baseUrl.EndsWith(ruta, StringComparison.OrdinalIgnoreCase)) return baseUrl;
		return baseUrl + ruta;
	}
}
