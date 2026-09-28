using ApiLinaAgbd.Models.Integracion;

namespace ApiLinaAgbd.Repositories.Integracion;

public interface IIntegracionRepository
{
    IntegracionConfiguracionDto? ObtenerConfiguracion(int? id = null);
    List<IntegracionConfiguracionDto> ListarConfiguraciones();
    int? ObtenerIdPorApiKey(string apiKey);
    int GuardarConfiguracion(IntegracionConfiguracionGuardarDto dto, int? id = null);
    (List<IntegracionProductoDto> Productos, List<IntegracionProveedorDto> Proveedores) ObtenerCatalogo(string apiKey, long auditoriaId);
    long IniciarAuditoria(string apiKey, string operacion, DateTime inicio, string? ip);
    void FinalizarAuditoria(long id, DateTime fin, long duracionMs, string estado, int registros, string? detalle);
    List<IntegracionAuditoriaDto> ListarAuditoria();
}
