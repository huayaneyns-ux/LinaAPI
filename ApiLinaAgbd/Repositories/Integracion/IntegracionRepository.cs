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
        using var cmd = new SqlCommand("SELECT TOP 1 Id, NombreEmpresa, Descripcion, ApiKey, Estado, DominioEndpoint, ApiKeyExterna FROM dbo.IntegracionEmpresa WHERE (@Id IS NULL OR Id = @Id) ORDER BY Id", con);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = (object?)id ?? DBNull.Value;
        using var dr = cmd.ExecuteReader();
        if (!dr.Read()) return null;
        var result = new IntegracionConfiguracionDto {
            Id = Convert.ToInt32(dr["Id"]), NombreEmpresa = Convert.ToString(dr["NombreEmpresa"]) ?? string.Empty,
            Descripcion = NullableString(dr["Descripcion"]), ApiKey = Convert.ToString(dr["ApiKey"]) ?? string.Empty,
            Estado = Convert.ToBoolean(dr["Estado"])
            ,DominioEndpoint = NullableString(dr["DominioEndpoint"]), ApiKeyExterna = NullableString(dr["ApiKeyExterna"])
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
        using var cmd = new SqlCommand("SELECT Id, NombreEmpresa, Descripcion, ApiKey, Estado, DominioEndpoint, ApiKeyExterna FROM dbo.IntegracionEmpresa WHERE Estado=1 ORDER BY NombreEmpresa", con);
        using var dr = cmd.ExecuteReader();
        var result = new List<IntegracionConfiguracionDto>();
        while (dr.Read()) result.Add(new IntegracionConfiguracionDto
        {
            Id = Convert.ToInt32(dr["Id"]),
            NombreEmpresa = Convert.ToString(dr["NombreEmpresa"]) ?? string.Empty,
            Descripcion = NullableString(dr["Descripcion"]),
            ApiKey = Convert.ToString(dr["ApiKey"]) ?? string.Empty,
            Estado = Convert.ToBoolean(dr["Estado"])
            ,DominioEndpoint = NullableString(dr["DominioEndpoint"]), ApiKeyExterna = NullableString(dr["ApiKeyExterna"])
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
            ? "UPDATE dbo.IntegracionEmpresa SET NombreEmpresa=@Nombre, Descripcion=@Descripcion, ApiKey=@ApiKey, Estado=@Estado, DominioEndpoint=@DominioEndpoint, ApiKeyExterna=@ApiKeyExterna, ActualizadoEn=SYSUTCDATETIME() WHERE Id=@Id"
            : "INSERT dbo.IntegracionEmpresa (NombreEmpresa, Descripcion, ApiKey, Estado, DominioEndpoint, ApiKeyExterna) OUTPUT INSERTED.Id VALUES (@Nombre,@Descripcion,@ApiKey,@Estado,@DominioEndpoint,@ApiKeyExterna)", con, tx))
        {
            cmd.Parameters.AddWithValue("@Nombre", dto.NombreEmpresa.Trim());
            cmd.Parameters.AddWithValue("@Descripcion", (object?)dto.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ApiKey", apiKey);
            cmd.Parameters.AddWithValue("@Estado", dto.Estado);
            cmd.Parameters.AddWithValue("@DominioEndpoint", (object?)dto.DominioEndpoint?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ApiKeyExterna", (object?)dto.ApiKeyExterna?.Trim() ?? DBNull.Value);
            if (id.HasValue) cmd.Parameters.AddWithValue("@Id", id.Value);
            result = id ?? Convert.ToInt32(cmd.ExecuteScalar());
            if (id.HasValue) cmd.ExecuteNonQuery();
        }
        if (!id.HasValue)
        {
            using var products = new SqlCommand(@"INSERT INTO dbo.IntegracionEmpresaProducto (IntegracionEmpresaId, ProductoId)
                SELECT @EmpresaId, p.id FROM dbo.Producto p WHERE p.estado=1", con, tx);
            products.Parameters.AddWithValue("@EmpresaId", result);
            products.ExecuteNonQuery();

            using var providers = new SqlCommand(@"INSERT INTO dbo.IntegracionEmpresaProveedor (IntegracionEmpresaId, ProveedorId)
                SELECT @EmpresaId, p.id FROM dbo.Proveedor p WHERE p.estado=1", con, tx);
            providers.Parameters.AddWithValue("@EmpresaId", result);
            providers.ExecuteNonQuery();
        }
        tx.Commit();
        return result;
    }

    public (List<IntegracionProductoDto> Productos, List<IntegracionProveedorDto> Proveedores) ObtenerCatalogo(string apiKey, long auditoriaId, string? tipo = null)
    {
        using var con = _conexion.ObtenerConexion();
        con.Open();
        using var tx = con.BeginTransaction(IsolationLevel.Serializable);
        var productos = new List<IntegracionProductoDto>();
        var proveedores = new List<IntegracionProveedorDto>();
        int empresaId;
        using (var idCmd = new SqlCommand("SELECT Id FROM dbo.IntegracionEmpresa WHERE ApiKey=@ApiKey AND Estado=1", con, tx))
        {
            idCmd.Parameters.AddWithValue("@ApiKey", apiKey);
            var value = idCmd.ExecuteScalar();
            if (value is null) throw new UnauthorizedAccessException("Clave de integración inválida o empresa inactiva.");
            empresaId = Convert.ToInt32(value);
        }
        if (!string.Equals(tipo, "PROVEEDORES", StringComparison.OrdinalIgnoreCase))
        using (var cmd = new SqlCommand(@"SELECT p.id,p.codigo,p.sku,p.nombre,p.descripcion,p.precio_venta,p.stockActual,c.nombre categoria
            FROM dbo.IntegracionEmpresaProducto ep INNER JOIN dbo.Producto p ON p.id=ep.ProductoId
            LEFT JOIN dbo.Categoria c ON c.id=p.id_categoria LEFT JOIN dbo.Proveedor pr ON pr.id=p.id_proveedor
            WHERE ep.IntegracionEmpresaId=@EmpresaId AND p.estado=1
              AND (p.id_integracion_sistema IS NULL OR p.id_integracion_sistema <> @EmpresaId)
              AND NOT EXISTS (SELECT 1 FROM dbo.IntegracionCatalogoEntrega ce WHERE ce.IntegracionEmpresaId=ep.IntegracionEmpresaId AND ce.TipoRegistro='P' AND ce.RegistroId=p.id AND ce.ConfirmadoEn IS NOT NULL)", con, tx))
        {
            cmd.Parameters.AddWithValue("@EmpresaId", empresaId);
            using var dr = cmd.ExecuteReader();
            while (dr.Read()) productos.Add(new IntegracionProductoDto { Id=Convert.ToInt32(dr["id"]), Codigo=NullableString(dr["codigo"]), Sku=NullableString(dr["sku"]), Nombre=NullableString(dr["nombre"]), Descripcion=NullableString(dr["descripcion"]), PrecioVenta=Convert.ToDecimal(dr["precio_venta"]), Stock=Convert.ToDecimal(dr["stockActual"]), Categoria=NullableString(dr["categoria"]) });
        }
        if (!string.Equals(tipo, "PRODUCTOS", StringComparison.OrdinalIgnoreCase))
        using (var cmd = new SqlCommand(@"SELECT DISTINCT pr.id,pr.ruc,pr.razon_social,pr.nombre_contacto,pr.telefono FROM dbo.IntegracionEmpresaProveedor ep INNER JOIN dbo.Proveedor pr ON pr.id=ep.ProveedorId WHERE ep.IntegracionEmpresaId=@EmpresaId AND pr.estado=1
            AND (pr.id_integracion_sistema IS NULL OR pr.id_integracion_sistema <> @EmpresaId)
            AND NOT EXISTS (SELECT 1 FROM dbo.IntegracionCatalogoEntrega ce WHERE ce.IntegracionEmpresaId=ep.IntegracionEmpresaId AND ce.TipoRegistro='V' AND ce.RegistroId=pr.id AND ce.ConfirmadoEn IS NOT NULL)", con, tx))
        {
            cmd.Parameters.AddWithValue("@EmpresaId", empresaId);
            using var dr = cmd.ExecuteReader();
            while (dr.Read()) proveedores.Add(new IntegracionProveedorDto { Id=Convert.ToInt32(dr["id"]), Ruc=NullableString(dr["ruc"]), RazonSocial=NullableString(dr["razon_social"]), NombreContacto=NullableString(dr["nombre_contacto"]), Telefono=NullableString(dr["telefono"]) });
        }
        using (var mark = new SqlCommand(@"IF NOT EXISTS (SELECT 1 FROM dbo.IntegracionCatalogoEntrega WHERE IntegracionEmpresaId=@EmpresaId AND TipoRegistro=@Tipo AND RegistroId=@RegistroId)
            INSERT INTO dbo.IntegracionCatalogoEntrega (IntegracionEmpresaId,TipoRegistro,RegistroId) VALUES (@EmpresaId,@Tipo,@RegistroId)", con, tx))
        {
            mark.Parameters.Add("@EmpresaId", SqlDbType.Int).Value = empresaId;
            mark.Parameters.Add("@Tipo", SqlDbType.Char, 1);
            mark.Parameters.Add("@RegistroId", SqlDbType.Int);
            if (!string.Equals(tipo, "PROVEEDORES", StringComparison.OrdinalIgnoreCase))
                foreach (var item in productos) { mark.Parameters["@Tipo"].Value = "P"; mark.Parameters["@RegistroId"].Value = item.Id; mark.ExecuteNonQuery(); }
            if (!string.Equals(tipo, "PRODUCTOS", StringComparison.OrdinalIgnoreCase))
                foreach (var item in proveedores) { mark.Parameters["@Tipo"].Value = "V"; mark.Parameters["@RegistroId"].Value = item.Id; mark.ExecuteNonQuery(); }
        }
        tx.Commit();
        return (productos, proveedores);
    }

    public int ConfirmarCatalogo(string apiKey, IntegracionCatalogoConfirmacionDto dto)
    {
        using var con = _conexion.ObtenerConexion(); con.Open(); using var tx = con.BeginTransaction();
        int empresaId;
        using (var company = new SqlCommand("SELECT Id FROM dbo.IntegracionEmpresa WHERE ApiKey=@ApiKey AND Estado=1", con, tx))
        {
            company.Parameters.AddWithValue("@ApiKey", apiKey);
            var value = company.ExecuteScalar();
            if (value is null) throw new UnauthorizedAccessException("Clave de integración inválida o empresa inactiva.");
            empresaId = Convert.ToInt32(value);
        }
        var confirmed = 0;
        using var update = new SqlCommand(@"UPDATE ce SET ConfirmadoEn=SYSUTCDATETIME()
            FROM dbo.IntegracionCatalogoEntrega ce
            WHERE ce.IntegracionEmpresaId=@EmpresaId AND ce.TipoRegistro=@Tipo AND ce.ConfirmadoEn IS NULL
              AND ce.RegistroId IN (SELECT TRY_CONVERT(INT, value) FROM STRING_SPLIT(@Ids, ','))", con, tx);
        update.Parameters.AddWithValue("@EmpresaId", empresaId);
        update.Parameters.Add("@Tipo", SqlDbType.Char, 1);
        update.Parameters.Add("@Ids", SqlDbType.VarChar, -1);
        if (dto.ProductoIds.Count > 0) { update.Parameters["@Tipo"].Value = "P"; update.Parameters["@Ids"].Value = string.Join(',', dto.ProductoIds.Distinct()); confirmed += update.ExecuteNonQuery(); }
        if (dto.ProveedorIds.Count > 0) { update.Parameters["@Tipo"].Value = "V"; update.Parameters["@Ids"].Value = string.Join(',', dto.ProveedorIds.Distinct()); confirmed += update.ExecuteNonQuery(); }
        tx.Commit();
        return confirmed;
    }

    public void EliminarConfiguracion(int id)
    {
        using var con = _conexion.ObtenerConexion(); con.Open();
        using var cmd = new SqlCommand(@"UPDATE dbo.IntegracionEmpresa
            SET Estado=0, ActualizadoEn=SYSUTCDATETIME()
            WHERE Id=@Id", con);
        cmd.Parameters.AddWithValue("@Id", id);
        if (cmd.ExecuteNonQuery() == 0)
            throw new KeyNotFoundException("La empresa de integración no existe.");
    }

    public IntegracionCatalogoAdminDto ObtenerCatalogoAdmin(int empresaId)
    {
        using var con = _conexion.ObtenerConexion(); con.Open();
        var result = new IntegracionCatalogoAdminDto();
        using (var cmd = new SqlCommand(@"SELECT p.id,p.codigo,p.sku,p.nombre,e.NombreEmpresa,
            CASE WHEN ep.ProductoId IS NULL THEN 0 ELSE 1 END Seleccionado,
            CASE WHEN ce.RegistroId IS NULL THEN 0 ELSE 1 END Entregado
            FROM dbo.Producto p LEFT JOIN dbo.IntegracionEmpresa e ON e.Id=p.id_integracion_sistema
            LEFT JOIN dbo.IntegracionEmpresaProducto ep ON ep.ProductoId=p.id AND ep.IntegracionEmpresaId=@EmpresaId
            LEFT JOIN dbo.IntegracionCatalogoEntrega ce ON ce.RegistroId=p.id AND ce.TipoRegistro='P' AND ce.IntegracionEmpresaId=@EmpresaId AND ce.ConfirmadoEn IS NOT NULL
            WHERE p.estado=1 AND (p.id_integracion_sistema IS NULL OR p.id_integracion_sistema <> @EmpresaId) ORDER BY p.nombre", con))
        { cmd.Parameters.AddWithValue("@EmpresaId", empresaId); using var dr=cmd.ExecuteReader(); while(dr.Read()) result.Productos.Add(new IntegracionProductoAdminDto { Id=Convert.ToInt32(dr["id"]), Codigo=NullableString(dr["codigo"]), Sku=NullableString(dr["sku"]), Nombre=NullableString(dr["nombre"]), EmpresaOrigen=NullableString(dr["NombreEmpresa"]), Seleccionado=Convert.ToBoolean(dr["Seleccionado"]), Entregado=Convert.ToBoolean(dr["Entregado"]) }); }
        using (var cmd = new SqlCommand(@"SELECT p.id,p.ruc,p.razon_social,e.NombreEmpresa,
            CASE WHEN ep.ProveedorId IS NULL THEN 0 ELSE 1 END Seleccionado,
            CASE WHEN ce.RegistroId IS NULL THEN 0 ELSE 1 END Entregado
            FROM dbo.Proveedor p LEFT JOIN dbo.IntegracionEmpresa e ON e.Id=p.id_integracion_sistema
            LEFT JOIN dbo.IntegracionEmpresaProveedor ep ON ep.ProveedorId=p.id AND ep.IntegracionEmpresaId=@EmpresaId
            LEFT JOIN dbo.IntegracionCatalogoEntrega ce ON ce.RegistroId=p.id AND ce.TipoRegistro='V' AND ce.IntegracionEmpresaId=@EmpresaId AND ce.ConfirmadoEn IS NOT NULL
            WHERE p.estado=1 AND (p.id_integracion_sistema IS NULL OR p.id_integracion_sistema <> @EmpresaId) ORDER BY p.razon_social", con))
        { cmd.Parameters.AddWithValue("@EmpresaId", empresaId); using var dr=cmd.ExecuteReader(); while(dr.Read()) result.Proveedores.Add(new IntegracionProveedorAdminDto { Id=Convert.ToInt32(dr["id"]), Ruc=NullableString(dr["ruc"]), RazonSocial=NullableString(dr["razon_social"]), EmpresaOrigen=NullableString(dr["NombreEmpresa"]), Seleccionado=Convert.ToBoolean(dr["Seleccionado"]), Entregado=Convert.ToBoolean(dr["Entregado"]) }); }
        return result;
    }

    public void GuardarSeleccionCatalogo(int empresaId, IntegracionCatalogoSeleccionGuardarDto dto)
    {
        using var con = _conexion.ObtenerConexion(); con.Open(); using var tx=con.BeginTransaction();
        using (var clear = new SqlCommand("DELETE FROM dbo.IntegracionEmpresaProducto WHERE IntegracionEmpresaId=@Id; DELETE FROM dbo.IntegracionEmpresaProveedor WHERE IntegracionEmpresaId=@Id;", con, tx)) { clear.Parameters.AddWithValue("@Id", empresaId); clear.ExecuteNonQuery(); }
        using var product = new SqlCommand("INSERT INTO dbo.IntegracionEmpresaProducto(IntegracionEmpresaId,ProductoId) VALUES(@EmpresaId,@RegistroId)", con, tx);
        product.Parameters.AddWithValue("@EmpresaId", empresaId); product.Parameters.Add("@RegistroId", SqlDbType.Int);
        foreach (var id in dto.ProductoIds.Distinct()) { product.Parameters["@RegistroId"].Value=id; product.ExecuteNonQuery(); }
        using var provider = new SqlCommand("INSERT INTO dbo.IntegracionEmpresaProveedor(IntegracionEmpresaId,ProveedorId) VALUES(@EmpresaId,@RegistroId)", con, tx);
        provider.Parameters.AddWithValue("@EmpresaId", empresaId); provider.Parameters.Add("@RegistroId", SqlDbType.Int);
        foreach (var id in dto.ProveedorIds.Distinct()) { provider.Parameters["@RegistroId"].Value=id; provider.ExecuteNonQuery(); }
        tx.Commit();
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
