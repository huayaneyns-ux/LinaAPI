using ApiLinaAgbd.Models.Integracion;
using ApiLinaAgbd.Repositories.Integracion;

namespace ApiLinaAgbd.Services.Integracion;

public sealed class IntegracionService
{
    private readonly IIntegracionRepository _repository;
    public IntegracionService(IIntegracionRepository repository) => _repository = repository;
    public IntegracionConfiguracionDto? Obtener(int? id = null) => _repository.ObtenerConfiguracion(id);
    public List<IntegracionConfiguracionDto> Listar() => _repository.ListarConfiguraciones();
    public int Guardar(IntegracionConfiguracionGuardarDto dto, int? id = null) => _repository.GuardarConfiguracion(dto, id);
    public List<IntegracionAuditoriaDto> Auditoria() => _repository.ListarAuditoria();
    public long IniciarAuditoria(string apiKey, string operacion, DateTime inicio, string? ip) => _repository.IniciarAuditoria(apiKey, operacion, inicio, ip);
    public void FinalizarAuditoria(long id, DateTime fin, long duracionMs, string estado, int registros, string? detalle) => _repository.FinalizarAuditoria(id, fin, duracionMs, estado, registros, detalle);
    public (long Id, List<IntegracionProductoDto> Productos, List<IntegracionProveedorDto> Proveedores, DateTime Inicio) Catalogo(string apiKey, string? ip)
    {
        var inicio = DateTime.UtcNow;
        var id = _repository.IniciarAuditoria(apiKey, "CONSULTA_CATALOGO", inicio, ip);
        try
        {
            var catalogo = _repository.ObtenerCatalogo(apiKey, id);
            _repository.FinalizarAuditoria(id, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "EXITOSO", catalogo.Productos.Count + catalogo.Proveedores.Count, null);
            return (id, catalogo.Productos, catalogo.Proveedores, inicio);
        }
        catch (Exception ex)
        {
            _repository.FinalizarAuditoria(id, DateTime.UtcNow, (long)(DateTime.UtcNow - inicio).TotalMilliseconds, "ERROR", 0, ex.Message[..Math.Min(ex.Message.Length, 1000)]);
            throw;
        }
    }
}
