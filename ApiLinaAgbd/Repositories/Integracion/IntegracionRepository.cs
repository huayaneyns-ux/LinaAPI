using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Integracion;

namespace ApiLinaAgbd.Repositories.Integracion;

public sealed class IntegracionRepository : IIntegracionRepository
{
    private readonly Conexion _conexion;
    public IntegracionRepository(Conexion conexion) => _conexion = conexion;

    public IntegracionConfiguracionDto? ObtenerConfiguracion(int? id = null)
    {
        using var con = _conexion.ObtenerConexion();
        con.Open();
        using var cmd = new SqlCommand("SELECT TOP 1 Id, NombreEmpresa, Descripcion, ApiKey, Estado FROM dbo.IntegracionEmpresa WHERE (@Id IS NULL OR Id = @Id) ORDER BY Id", con);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = (object?)id ?? DBNull.Value;
        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return null;
        var result = new IntegracionConfiguracionDto {
            Id = Convert.ToInt32(dr["Id"]), NombreEmpresa = Convert.ToString(dr["NombreEmpresa"]) ?? string.Empty,
            Descripcion = NullableString(dr["Descripcion"]), ApiKey = Convert.ToString(dr["ApiKey"]) ?? string.Empty,
            Estado = Convert.ToBoolean(dr["Estado"])
        };
        return result;
    }

    public int? ObtenerIdPorApiKey(string apiKey)
    {
        using var con = _conexion.ObtenerConexion();
        con.Open();
        using var cmd = new SqlCommand("SELECT TOP 1 Id FROM dbo.IntegracionEmpresa WHERE ApiKey=@ApiKey AND Estado=1", con);
        cmd.Parameters.AddWithValue("@ApiKey", apiKey);
        var value = cmd.ExecuteScalar();
        return value is null ? null : Convert.ToInt32(value);
    }

    public List<IntegracionConfiguracionDto> ListarConfiguraciones()
    {
        using var con = _conexion.ObtenerConexion();
        con.Open();
        using var cmd = new SqlCommand("SELECT Id, NombreEmpresa, Descripcion, ApiKey, Estado FROM dbo.IntegracionEmpresa ORDER BY NombreEmpresa", con);
        using var dr = cmd.ExecuteReader();
        var result = new List<IntegracionConfiguracionDto>();
        while (dr.Read()) result.Add(new IntegracionConfiguracionDto
        {
            Id = Convert.ToInt32(dr["Id"]),
            NombreEmpresa = Convert.ToString(dr["NombreEmpresa"]) ?? string.Empty,
            Descripcion = NullableString(dr["Descripcion"]),
            ApiKey = Convert.ToString(dr["ApiKey"]) ?? string.Empty,
            Estado = Convert.ToBoolean(dr["Estado"])
        });
        return result;
    }

    public int GuardarConfiguracion(IntegracionConfiguracionGuardarDto dto, int? id = null)
    {
        using var con = _conexion.ObtenerConexion();
        con.Open();
        using var tx = con.BeginTransaction();
        int result;
        var apiKey = string.IsNullOrWhiteSpace(dto.ApiKey) ? $"lina-{Guid.NewGuid():N}" : dto.ApiKey.Trim();
        using (var cmd = new SqlCommand(id.HasValue
            ? "UPDATE dbo.IntegracionEmpresa SET NombreEmpresa=@Nombre, Descripcion=@Descripcion, ApiKey=@ApiKey, Estado=@Estado, ActualizadoEn=SYSUTCDATETIME() WHERE Id=@Id"
            : "INSERT dbo.IntegracionEmpresa (NombreEmpresa, Descripcion, ApiKey, Estado) OUTPUT INSERTED.Id VALUES (@Nombre,@Descripcion,@ApiKey,@Estado)", con, tx))
        {
            cmd.Parameters.AddWithValue("@Nombre", dto.NombreEmpresa.Trim());
            cmd.Parameters.AddWithValue("@Descripcion", (object?)dto.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ApiKey", apiKey);
            cmd.Parameters.AddWithValue("@Estado", dto.Estado);
            if (id.HasValue) cmd.Parameters.AddWithValue("@Id", id.Value);
            result = id ?? Convert.ToInt32(cmd.ExecuteScalar());
            if (id.HasValue) cmd.ExecuteNonQuery();
        }
        tx.Commit();
        return result;
    }

    public (List<IntegracionProductoDto> Productos, List<IntegracionProveedorDto> Proveedores) ObtenerCatalogo(string apiKey, long auditoriaId)
    {
        using var con = _conexion.ObtenerConexion();
        con.Open();
        var productos = new List<IntegracionProductoDto>();
        var proveedores = new List<IntegracionProveedorDto>();
        using (var cmd = new SqlCommand(@"SELECT p.id,p.codigo,p.sku,p.nombre,p.descripcion,p.precio_venta,p.stockActual,p.id_integracion_sistema,c.nombre categoria,pr.ruc,pr.razon_social
            FROM dbo.IntegracionEmpresa e INNER JOIN dbo.Producto p ON p.id_integracion_sistema=e.Id
            LEFT JOIN dbo.Categoria c ON c.id=p.id_categoria LEFT JOIN dbo.Proveedor pr ON pr.id=p.id_proveedor
            WHERE e.ApiKey=@ApiKey AND e.Estado=1 AND p.estado=1", con))
        {
            cmd.Parameters.AddWithValue("@ApiKey", apiKey);
            using var dr = cmd.ExecuteReader();
            while (dr.Read()) productos.Add(new IntegracionProductoDto { Id=Convert.ToInt32(dr["id"]), Codigo=NullableString(dr["codigo"]), Sku=NullableString(dr["sku"]), Nombre=NullableString(dr["nombre"]), Descripcion=NullableString(dr["descripcion"]), PrecioVenta=Convert.ToDecimal(dr["precio_venta"]), Stock=Convert.ToDecimal(dr["stockActual"]), IdIntegracionSistema=dr["id_integracion_sistema"] == DBNull.Value ? null : Convert.ToInt32(dr["id_integracion_sistema"]), Categoria=NullableString(dr["categoria"]), Ruc=NullableString(dr["ruc"]), RazonSocial=NullableString(dr["razon_social"]) });
        }
        using (var cmd = new SqlCommand(@"SELECT DISTINCT pr.id,pr.ruc,pr.razon_social,pr.nombre_contacto,pr.telefono FROM dbo.IntegracionEmpresa e INNER JOIN dbo.Proveedor pr ON pr.id_integracion_sistema=e.Id WHERE e.ApiKey=@ApiKey AND e.Estado=1 AND pr.estado=1", con))
        {
            cmd.Parameters.AddWithValue("@ApiKey", apiKey);
            using var dr = cmd.ExecuteReader();
            while (dr.Read()) proveedores.Add(new IntegracionProveedorDto { Id=Convert.ToInt32(dr["id"]), Ruc=NullableString(dr["ruc"]), RazonSocial=NullableString(dr["razon_social"]), NombreContacto=NullableString(dr["nombre_contacto"]), Telefono=NullableString(dr["telefono"]) });
        }
        return (productos, proveedores);
    }

    public long IniciarAuditoria(string apiKey, string operacion, DateTime inicio, string? ip)
    {
        using var con = _conexion.ObtenerConexion(); con.Open();
        using var cmd = new SqlCommand(@"INSERT dbo.IntegracionAuditoria (IntegracionEmpresaId,Operacion,FechaInicio,Estado,IpOrigen) OUTPUT INSERTED.Id SELECT Id,@Operacion,@Inicio,'EN_PROCESO',@Ip FROM dbo.IntegracionEmpresa WHERE ApiKey=@ApiKey AND Estado=1", con);
        cmd.Parameters.AddWithValue("@ApiKey", apiKey); cmd.Parameters.AddWithValue("@Operacion", operacion); cmd.Parameters.AddWithValue("@Inicio", inicio); cmd.Parameters.AddWithValue("@Ip", (object?)ip ?? DBNull.Value);
        var result = cmd.ExecuteScalar();
        if (result is null) throw new UnauthorizedAccessException("Clave de integración inválida o empresa inactiva.");
        return Convert.ToInt64(result);
    }

    public void FinalizarAuditoria(long id, DateTime fin, long duracionMs, string estado, int registros, string? detalle)
    {
        using var con = _conexion.ObtenerConexion(); con.Open();
        using var cmd = new SqlCommand("UPDATE dbo.IntegracionAuditoria SET FechaFin=@Fin,DuracionMs=@Duracion,Estado=@Estado,RegistrosEnviados=@Registros,Detalle=@Detalle WHERE Id=@Id", con);
        cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@Fin", fin); cmd.Parameters.AddWithValue("@Duracion", duracionMs); cmd.Parameters.AddWithValue("@Estado", estado); cmd.Parameters.AddWithValue("@Registros", registros); cmd.Parameters.AddWithValue("@Detalle", (object?)detalle ?? DBNull.Value); cmd.ExecuteNonQuery();
    }

    public List<IntegracionAuditoriaDto> ListarAuditoria()
    {
        using var con = _conexion.ObtenerConexion(); con.Open();
        using var cmd = new SqlCommand("SELECT TOP 500 a.Id,a.IntegracionEmpresaId,COALESCE(e.NombreEmpresa, 'Empresa eliminada') AS Empresa,a.Operacion,a.FechaInicio,a.FechaFin,a.DuracionMs,a.Estado,a.RegistrosEnviados,a.Detalle,a.IpOrigen FROM dbo.IntegracionAuditoria a LEFT JOIN dbo.IntegracionEmpresa e ON e.Id=a.IntegracionEmpresaId ORDER BY a.FechaInicio DESC", con);
        using var dr = cmd.ExecuteReader(); var result = new List<IntegracionAuditoriaDto>();
        while (dr.Read()) result.Add(new IntegracionAuditoriaDto { Id=Convert.ToInt64(dr["Id"]), IntegracionEmpresaId=dr["IntegracionEmpresaId"] == DBNull.Value ? null : Convert.ToInt32(dr["IntegracionEmpresaId"]), Empresa=Convert.ToString(dr["Empresa"]) ?? "", Operacion=Convert.ToString(dr["Operacion"]) ?? "", FechaInicio=Convert.ToDateTime(dr["FechaInicio"]), FechaFin=dr["FechaFin"] == DBNull.Value ? null : Convert.ToDateTime(dr["FechaFin"]), DuracionMs=dr["DuracionMs"] == DBNull.Value ? null : Convert.ToInt64(dr["DuracionMs"]), Estado=Convert.ToString(dr["Estado"]) ?? "", RegistrosEnviados=Convert.ToInt32(dr["RegistrosEnviados"]), Detalle=NullableString(dr["Detalle"]), IpOrigen=NullableString(dr["IpOrigen"]) });
        return result;
    }

    private static string? NullableString(object value) => value == DBNull.Value ? null : Convert.ToString(value);
}
