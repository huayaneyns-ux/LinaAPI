using ApiLinaAgbd.Models.Integracion;

namespace ApiLinaAgbd.Repositories.Integracion;

public interface IIntegracionRepository
{
    IntegracionConfiguracionDto? ObtenerConfiguracion(int? id = null);
    List<IntegracionConfiguracionDto> ListarConfiguraciones();
    int? ObtenerIdPorApiKey(string apiKey);
    int GuardarConfiguracion(IntegracionConfiguracionGuardarDto dto, int? id = null);
    (List<IntegracionProductoDto> Productos, List<IntegracionProveedorDto> Proveedores) ObtenerCatalogo(string apiKey, long auditoriaId, string? tipo = null);
    long IniciarAuditoria(string apiKey, string operacion, DateTime inicio, string? ip);
    void FinalizarAuditoria(long id, DateTime fin, long duracionMs, string estado, int registros, string? detalle);
    List<IntegracionAuditoriaDto> ListarAuditoria();
    IntegracionCatalogoAdminDto ObtenerCatalogoAdmin(int empresaId);
    void GuardarSeleccionCatalogo(int empresaId, IntegracionCatalogoSeleccionGuardarDto dto);
    int ConfirmarCatalogo(string apiKey, IntegracionCatalogoConfirmacionDto dto);
    void EliminarConfiguracion(int id);
    int GuardarConsultaExterna(int empresaId, List<IntegracionProductoDto> productos, List<IntegracionProveedorDto> proveedores, List<ClienteIntegracionDto> clientes);
}
