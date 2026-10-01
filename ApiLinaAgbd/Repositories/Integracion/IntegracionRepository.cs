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

    public IntegracionImportacionResultadoDto GuardarConsultaExterna(int empresaId, List<IntegracionProductoDto> productos, List<IntegracionProveedorDto> proveedores, List<ClienteIntegracionDto> clientes)
    {
        using var con = _conexion.ObtenerConexion(); con.Open(); using var tx = con.BeginTransaction();
        var resultado = new IntegracionImportacionResultadoDto();
        var proveedorIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var proveedor in proveedores)
        {
            var razon = (proveedor.RazonSocial ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(razon)) continue;
            var ruc = string.IsNullOrWhiteSpace(proveedor.Ruc) ? $"EXT-{empresaId}-{proveedor.Id}" : proveedor.Ruc.Trim();
            var upsert = UpsertProveedor(con, tx, empresaId, ruc, razon, proveedor.NombreContacto, proveedor.Telefono);
            proveedorIds[razon] = upsert.Id; RegistrarEstado(resultado, upsert.Estado);
        }

        var proveedorGenerico = proveedores.FirstOrDefault()?.RazonSocial?.Trim();
        var proveedorGenericoId = proveedorGenerico is null
            ? UpsertProveedor(con, tx, empresaId, $"EXT-{empresaId}-GEN", $"Proveedor externo - empresa {empresaId}", null, null).Id
            : proveedorIds[proveedorGenerico];
        foreach (var producto in productos)
        {
            var codigo = (producto.Codigo ?? string.Empty).Trim();
            var nombre = (producto.Nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre)) continue;
            var categoriaId = UpsertNombre(con, tx, "Categoria", producto.Categoria, "Sin categoría");
            var marcaId = UpsertNombre(con, tx, "Marca", producto.Marca, "Sin marca");
            var unidadId = UpsertUnidad(con, tx, producto.Unidad);
            var sku = (producto.Sku ?? $"EXT-{empresaId}-{producto.Id}").Trim();
            if (sku.Length > 50) sku = sku[..50];
            using var cmd = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.Producto WHERE codigo=@Codigo)
                BEGIN
                  IF EXISTS (SELECT 1 FROM dbo.Producto WHERE codigo=@Codigo AND (nombre<>@Nombre OR ISNULL(descripcion,'')<>ISNULL(@Descripcion,'') OR sku<>@Sku OR precio_venta<>@Precio OR stockActual<>@Stock OR id_categoria<>@Categoria OR id_proveedor<>@Proveedor OR id_marca<>@Marca OR id_unidad_medida<>@Unidad OR estado<>1 OR ISNULL(id_integracion_sistema,0)<>@Empresa))
                  BEGIN UPDATE dbo.Producto SET nombre=@Nombre, descripcion=@Descripcion, sku=@Sku, precio_venta=@Precio, stockActual=@Stock, id_categoria=@Categoria, id_proveedor=@Proveedor, id_marca=@Marca, id_unidad_medida=@Unidad, id_integracion_sistema=@Empresa, estado=1 WHERE codigo=@Codigo; SELECT 2; END
                  ELSE SELECT 0;
                END
                ELSE BEGIN INSERT dbo.Producto(nombre,descripcion,sku,precio_venta,factor_conversion,stock_minimo,estado,id_categoria,id_proveedor,id_marca,id_unidad_medida,codigo,stockActual,id_integracion_sistema) VALUES(@Nombre,@Descripcion,@Sku,@Precio,1,0,1,@Categoria,@Proveedor,@Marca,@Unidad,@Codigo,@Stock,@Empresa); SELECT 1; END", con, tx);
            cmd.Parameters.AddWithValue("@Codigo", codigo); cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@Descripcion", (object?)producto.Descripcion ?? DBNull.Value); cmd.Parameters.AddWithValue("@Sku", sku);
            cmd.Parameters.AddWithValue("@Precio", producto.PrecioVenta); cmd.Parameters.AddWithValue("@Stock", producto.Stock);
            cmd.Parameters.AddWithValue("@Categoria", categoriaId); cmd.Parameters.AddWithValue("@Proveedor", proveedorGenericoId);
            cmd.Parameters.AddWithValue("@Marca", marcaId); cmd.Parameters.AddWithValue("@Unidad", unidadId); cmd.Parameters.AddWithValue("@Empresa", empresaId);
            RegistrarEstado(resultado, Convert.ToInt32(cmd.ExecuteScalar()));
        }

        foreach (var cliente in clientes)
        {
            var nombre = (cliente.nombre ?? string.Empty).Trim(); var correo = (cliente.email ?? string.Empty).Trim();
            var documento = (cliente.documento ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre)) continue;
            if (string.IsNullOrWhiteSpace(documento)) documento = $"EXT-{empresaId}-{cliente.id ?? Guid.NewGuid().GetHashCode():X}";
            using var cmd = new SqlCommand(@"DECLARE @IdDocumento INT, @IdUsuario INT, @Rol INT;
                SELECT TOP 1 @IdUsuario=u.id FROM dbo.usuario u LEFT JOIN dbo.documento d ON d.id=u.id_documento
                    WHERE (NULLIF(@Documento,'') IS NOT NULL AND d.numero=@Documento) OR (NULLIF(@Correo,'') IS NOT NULL AND LOWER(u.correo)=LOWER(@Correo));
                SELECT TOP 1 @Rol=id FROM dbo.rol WHERE UPPER(nombre)='CLIENTE' AND estado=1;
                IF @Rol IS NULL THROW 50041, 'No existe el rol CLIENTE.', 1;
                SELECT @IdDocumento=id FROM dbo.documento WHERE numero=@Documento;
                IF @IdDocumento IS NULL BEGIN INSERT dbo.documento(tipo_documento,numero,nombre) VALUES(CASE WHEN LEN(@Documento)=11 THEN 'RUC' ELSE 'DNI' END,@Documento,@Nombre); SET @IdDocumento=SCOPE_IDENTITY(); END;
                IF @IdUsuario IS NULL BEGIN INSERT dbo.usuario(nombre_apellido,telefono,correo,contrasena,estado,id_rol,id_documento,id_integracion_sistema) VALUES(@Nombre,@Telefono,@Correo,CONVERT(VARCHAR(36),NEWID()),1,@Rol,@IdDocumento,@Empresa); SELECT 1; END
                ELSE IF EXISTS (SELECT 1 FROM dbo.usuario WHERE id=@IdUsuario AND (ISNULL(nombre_apellido,'')<>@Nombre OR ISNULL(telefono,'')<>ISNULL(@Telefono,'') OR ISNULL(correo,'')<>@Correo OR estado<>1 OR id_rol<>@Rol OR ISNULL(id_documento,0)<>@IdDocumento OR ISNULL(id_integracion_sistema,0)<>@Empresa))
                BEGIN UPDATE dbo.usuario SET nombre_apellido=@Nombre,telefono=@Telefono,correo=@Correo,estado=1,id_rol=@Rol,id_documento=@IdDocumento,id_integracion_sistema=@Empresa WHERE id=@IdUsuario; SELECT 2; END
                ELSE SELECT 0;", con, tx);
            cmd.Parameters.AddWithValue("@Documento", documento); cmd.Parameters.AddWithValue("@Nombre", nombre);
            cmd.Parameters.AddWithValue("@Telefono", (object?)cliente.telefono ?? DBNull.Value); cmd.Parameters.AddWithValue("@Correo", correo);
            cmd.Parameters.AddWithValue("@Empresa", empresaId); RegistrarEstado(resultado, Convert.ToInt32(cmd.ExecuteScalar()));
        }
        tx.Commit(); resultado.Total = resultado.Insertados + resultado.Actualizados + resultado.SinCambios; return resultado;
    }

    private static void RegistrarEstado(IntegracionImportacionResultadoDto resultado, int estado)
    {
        if (estado == 1) resultado.Insertados++;
        else if (estado == 2) resultado.Actualizados++;
        else resultado.SinCambios++;
    }

    private static (int Id, int Estado) UpsertProveedor(SqlConnection con, SqlTransaction tx, int empresaId, string ruc, string razon, string? contacto, string? telefono)
    {
        using var cmd = new SqlCommand(@"DECLARE @Id INT; SELECT TOP 1 @Id=id FROM dbo.Proveedor WHERE LOWER(razon_social)=LOWER(@Razon) OR ruc=@Ruc;
            IF @Id IS NULL BEGIN INSERT dbo.Proveedor(ruc,razon_social,nombre_contacto,telefono,estado,id_integracion_sistema) VALUES(@Ruc,@Razon,@Contacto,@Telefono,1,@Empresa); SET @Id=SCOPE_IDENTITY(); SELECT CONCAT(@Id,':1'); END
            ELSE IF EXISTS (SELECT 1 FROM dbo.Proveedor WHERE id=@Id AND (ruc<>@Ruc OR razon_social<>@Razon OR ISNULL(nombre_contacto,'')<>ISNULL(@Contacto,'') OR ISNULL(telefono,'')<>ISNULL(@Telefono,'') OR estado<>1 OR ISNULL(id_integracion_sistema,0)<>@Empresa)) BEGIN UPDATE dbo.Proveedor SET ruc=@Ruc,razon_social=@Razon,nombre_contacto=@Contacto,telefono=@Telefono,estado=1,id_integracion_sistema=@Empresa WHERE id=@Id; SELECT CONCAT(@Id,':2'); END ELSE SELECT CONCAT(@Id,':0');", con, tx);
        cmd.Parameters.AddWithValue("@Ruc", ruc); cmd.Parameters.AddWithValue("@Razon", razon); cmd.Parameters.AddWithValue("@Contacto", (object?)contacto ?? DBNull.Value); cmd.Parameters.AddWithValue("@Telefono", (object?)telefono ?? DBNull.Value); cmd.Parameters.AddWithValue("@Empresa", empresaId);
        var parts = Convert.ToString(cmd.ExecuteScalar())!.Split(':'); return (Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
    }

    private static int UpsertNombre(SqlConnection con, SqlTransaction tx, string tabla, string? nombre, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(nombre) ? fallback : nombre.Trim();
        using var cmd = new SqlCommand($"DECLARE @Id INT; SELECT TOP 1 @Id=id FROM dbo.{tabla} WHERE LOWER(nombre)=LOWER(@Nombre); IF @Id IS NULL BEGIN INSERT dbo.{tabla}(nombre,estado) VALUES(@Nombre,1); SET @Id=SCOPE_IDENTITY(); END SELECT @Id;", con, tx);
        cmd.Parameters.AddWithValue("@Nombre", value.Length > 50 ? value[..50] : value); return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int UpsertUnidad(SqlConnection con, SqlTransaction tx, string? unidad)
    {
        var value = string.IsNullOrWhiteSpace(unidad) ? "unidad" : unidad.Trim();
        var abrev = value.ToLowerInvariant() switch { "m2" or "metro cuadrado" => "MTK", "kg" or "kilogramo" or "kilogramos" => "KGM", "m" or "metro" or "metros" => "MTR", _ => "NIU" };
        using var cmd = new SqlCommand(@"DECLARE @Id INT; SELECT TOP 1 @Id=id FROM dbo.UnidadMedida WHERE abreviatura=@Abrev OR LOWER(nombre)=LOWER(@Nombre); IF @Id IS NULL BEGIN INSERT dbo.UnidadMedida(nombre,abreviatura,estado) VALUES(@Nombre,@Abrev,1); SET @Id=SCOPE_IDENTITY(); END SELECT @Id;", con, tx);
        cmd.Parameters.AddWithValue("@Nombre", value.Length > 50 ? value[..50] : value); cmd.Parameters.AddWithValue("@Abrev", abrev); return Convert.ToInt32(cmd.ExecuteScalar());
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
