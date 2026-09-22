using ApiLinaAgbd.Models.Integracion;
using ApiLinaAgbd.Models.Seguridad;
using ApiLinaAgbd.Repositories.Seguridad.Usuario;

namespace ApiLinaAgbd.Services.Integracion;

public class IntegracionClientesService : IIntegracionClientesService
{
	private const int RolCliente = 1;
	private readonly IUsuarioRepository _usuarioRepository;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IConfiguration _configuration;
	private readonly ILogger<IntegracionClientesService> _logger;

	public IntegracionClientesService(
		IUsuarioRepository usuarioRepository,
		IHttpClientFactory httpClientFactory,
		IConfiguration configuration,
		ILogger<IntegracionClientesService> logger)
	{
		_usuarioRepository = usuarioRepository;
		_httpClientFactory = httpClientFactory;
		_configuration = configuration;
		_logger = logger;
	}

	public List<ClienteIntegracionDto> ListarClientes()
	{
		return _usuarioRepository.Listar()
			.Where(usuario => usuario.idRol == RolCliente)
			.Select(Mapear)
			.ToList();
	}

	public (ClienteIntegracionDto Cliente, bool YaExistia) RegistrarClienteExterno(ClienteWebhookDto cliente)
	{
		var documento = cliente.documento?.Trim();
		var tipoDocumento = string.IsNullOrWhiteSpace(cliente.tipoDocumento)
			? (documento?.Length == 11 ? "RUC" : "DNI")
			: cliente.tipoDocumento.Trim().ToUpperInvariant();
		var existente = string.IsNullOrWhiteSpace(documento)
			? null
			: _usuarioRepository.ObtenerPorDocumento(tipoDocumento, documento);

		if (existente is not null && existente.idRol == RolCliente)
		{
			return (Mapear(existente), true);
		}

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
			origen = string.IsNullOrWhiteSpace(cliente.origen) ? "acabados_js" : cliente.origen.Trim()
		});

		var registrado = _usuarioRepository.Obtener(id)
			?? throw new InvalidOperationException("No se pudo recuperar el cliente recién registrado.");
		return (Mapear(registrado), false);
	}

	public async Task NotificarClienteNuevoAsync(ClienteIntegracionDto cliente)
	{
		var url = _configuration["INTEGRACION_URL_WEBHOOK_ACABADOS"]?.Trim();
		var apiKey = _configuration["INTEGRACION_API_KEY_ACABADOS"]?.Trim();

		if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(apiKey))
		{
			_logger.LogWarning("Webhook de Acabados no configurado. Defina INTEGRACION_URL_WEBHOOK_ACABADOS e INTEGRACION_API_KEY_ACABADOS.");
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
			origen = "lina"
		};

		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Add("X-API-Key", apiKey);
			request.Content = JsonContent.Create(payload);

			var client = _httpClientFactory.CreateClient("IntegracionClientes");
			using var response = await client.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("Acabados respondió {StatusCode} al registrar el cliente {Documento}.", response.StatusCode, cliente.documento);
			}
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "No se pudo notificar el cliente {Documento} a Acabados.", cliente.documento);
		}
	}

	private static ClienteIntegracionDto Mapear(UsuarioSelectDto usuario) => new()
	{
		id = usuario.id,
		nombre = usuario.nombreApellido,
		documento = usuario.dni,
		tipoDocumento = usuario.tipoDocumento,
		telefono = usuario.telefono,
		email = usuario.correo,
		direccion = null,
		origen = usuario.origen
	};
}
