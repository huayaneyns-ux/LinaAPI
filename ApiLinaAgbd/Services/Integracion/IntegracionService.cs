using ApiLinaAgbd.Models.Integracion;
using ApiLinaAgbd.Repositories.Integracion;

namespace ApiLinaAgbd.Services.Integracion;

public sealed class IntegracionService
{
    private readonly IIntegracionRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    public IntegracionService(IIntegracionRepository repository, IHttpClientFactory httpClientFactory) { _repository = repository; _httpClientFactory = httpClientFactory; }
    public IntegracionConfiguracionDto? Obtener(int? id = null) => _repository.ObtenerConfiguracion(id);
    public List<IntegracionConfiguracionDto> Listar() => _repository.ListarConfiguraciones();
    public int Guardar(IntegracionConfiguracionGuardarDto dto, int? id = null) => _repository.GuardarConfiguracion(dto, id);
    public List<IntegracionAuditoriaDto> Auditoria() => _repository.ListarAuditoria();
    public IntegracionCatalogoAdminDto CatalogoAdmin(int empresaId) => _repository.ObtenerCatalogoAdmin(empresaId);
    public void GuardarSeleccionCatalogo(int empresaId, IntegracionCatalogoSeleccionGuardarDto dto) => _repository.GuardarSeleccionCatalogo(empresaId, dto);
    public long IniciarAuditoria(string apiKey, string operacion, DateTime inicio, string? ip) => _repository.IniciarAuditoria(apiKey, operacion, inicio, ip);
    public void FinalizarAuditoria(long id, DateTime fin, long duracionMs, string estado, int registros, string? detalle) => _repository.FinalizarAuditoria(id, fin, duracionMs, estado, registros, detalle);
    public int ConfirmarCatalogo(string apiKey, IntegracionCatalogoConfirmacionDto dto, string? ip)
    {
        var inicio = DateTime.UtcNow;
        var auditoriaId = _repository.IniciarAuditoria(apiKey, "CONFIRMACION_CATALOGO", inicio, ip);
        try
        {
            var confirmado = _repository.ConfirmarCatalogo(apiKey, dto);
            _repository.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds,
                "EXITOSO", confirmado, null);
            return confirmado;
        }
        catch (Exception ex)
        {
            _repository.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds,
                "ERROR", 0, ex.Message[..Math.Min(ex.Message.Length, 1000)]);
            throw;
        }
    }
    public void EliminarConfiguracion(int id) => _repository.EliminarConfiguracion(id);

    public async Task<IntegracionConsultaExternaDto> ConsultarEmpresaExternaAsync(int empresaId, string tipo, string? ip)
    {
        var empresa = _repository.ObtenerConfiguracion(empresaId)
            ?? throw new KeyNotFoundException("La empresa de integración no existe.");
        if (string.IsNullOrWhiteSpace(empresa.DominioEndpoint) || string.IsNullOrWhiteSpace(empresa.ApiKeyExterna))
            throw new InvalidOperationException("Configure el dominio/end­point y la clave externa antes de consultar.");

        var operacion = tipo.Equals("PROVEEDORES", StringComparison.OrdinalIgnoreCase)
            ? "CONSULTA_EXTERNA_PROVEEDORES" : "CONSULTA_EXTERNA_PRODUCTOS";
        var inicio = DateTime.UtcNow;
        var auditoriaId = _repository.IniciarAuditoria(empresa.ApiKey, operacion, inicio, ip);
        try
        {
            var client = _httpClientFactory.CreateClient("IntegracionExterna");
            var ruta = tipo.ToUpperInvariant() switch
            {
                "PROVEEDORES" => "/api/v1/proveedores",
                "CLIENTES" => "/api/v1/clientes",
                _ => "/api/v1/productos"
            };
            using var request = new HttpRequestMessage(HttpMethod.Get, ConstruirUrl(empresa.DominioEndpoint, ruta));
            request.Headers.Add("X-API-Key", empresa.ApiKeyExterna);
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"La empresa externa respondió {(int)response.StatusCode}: {body[..Math.Min(body.Length, 400)]}");

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var catalogo = tipo.Equals("CLIENTES", StringComparison.OrdinalIgnoreCase)
                ? new IntegracionRespuestaCatalogoDto { Clientes = System.Text.Json.JsonSerializer.Deserialize<List<ClienteIntegracionDto>>(body, options) ?? new() }
                : System.Text.Json.JsonSerializer.Deserialize<IntegracionRespuestaCatalogoDto>(body, options) ?? new();
            var clientesExternos = catalogo.Clientes
                .Where(cliente => !string.Equals(cliente.origen, "lina", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var importacion = _repository.GuardarConsultaExterna(empresaId, catalogo.Productos, catalogo.Proveedores, clientesExternos);
            var result = new IntegracionConsultaExternaDto
            {
                Tipo = tipo.ToUpperInvariant(),
                Productos = tipo.Equals("PROVEEDORES", StringComparison.OrdinalIgnoreCase) ? new() : catalogo.Productos,
                Proveedores = tipo.Equals("PROVEEDORES", StringComparison.OrdinalIgnoreCase) ? catalogo.Proveedores : new(),
                Clientes = catalogo.Clientes,
                Guardados = importacion.Total,
                Insertados = importacion.Insertados,
                Actualizados = importacion.Actualizados,
                SinCambios = importacion.SinCambios
            };
            _repository.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds,
                "EXITOSO", importacion.Total, null);
            return result;
        }
        catch (Exception ex)
        {
            _repository.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds,
                "ERROR", 0, ex.Message[..Math.Min(ex.Message.Length, 1000)]);
            throw;
        }
    }

    private sealed class IntegracionRespuestaCatalogoDto
    {
        public List<IntegracionProductoDto> Productos { get; set; } = new();
        public List<IntegracionProveedorDto> Proveedores { get; set; } = new();
        public List<ClienteIntegracionDto> Clientes { get; set; } = new();
    }

    private static string ConstruirUrl(string dominio, string ruta)
    {
        var baseUrl = dominio.TrimEnd('/');
        if (baseUrl.EndsWith(ruta, StringComparison.OrdinalIgnoreCase)) return baseUrl;
        return baseUrl + (baseUrl.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase) ? ruta.Replace("/api/v1", "", StringComparison.OrdinalIgnoreCase) : ruta);
    }
    public (long Id, List<IntegracionProductoDto> Productos, List<IntegracionProveedorDto> Proveedores, DateTime Inicio) Catalogo(string apiKey, string? ip, string? tipo = null)
    {
        var inicio = DateTime.UtcNow;
        var operacion = tipo is null ? "CONSULTA_CATALOGO" : $"CONSULTA_CATALOGO_{tipo}";
        var id = _repository.IniciarAuditoria(apiKey, operacion, inicio, ip);
        try
        {
            var catalogo = _repository.ObtenerCatalogo(apiKey, id, tipo);
            _repository.FinalizarAuditoria(id, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "EXITOSO", catalogo.Productos.Count + catalogo.Proveedores.Count, null);
            return (id, catalogo.Productos, catalogo.Proveedores, inicio);
        }
        catch (Exception ex)
        {
            _repository.FinalizarAuditoria(id, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "ERROR", 0, ex.Message[..Math.Min(ex.Message.Length, 1000)]);
            throw;
        }
    }

    public List<ClienteIntegracionDto> Clientes(string apiKey, string? ip)
    {
        var inicio = DateTime.UtcNow;
        var auditoriaId = _repository.IniciarAuditoria(apiKey, "CONSULTA_CATALOGO_CLIENTES", inicio, ip);
        try
        {
            var clientes = _repository.ObtenerClientesExpuestos(apiKey, auditoriaId);
            _repository.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "EXITOSO", clientes.Count, null);
            return clientes;
        }
        catch (Exception ex)
        {
            _repository.FinalizarAuditoria(auditoriaId, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "ERROR", 0, ex.Message[..Math.Min(ex.Message.Length, 1000)]);
            throw;
        }
    }
}
