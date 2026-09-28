namespace ApiLinaAgbd.Models.Integracion;

public sealed class IntegracionProductoDto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string? Sku { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Stock { get; set; }
    public int? IdIntegracionSistema { get; set; }
    public string? Categoria { get; set; }
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
}

public sealed class IntegracionProveedorDto
{
    public int Id { get; set; }
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreContacto { get; set; }
    public string? Telefono { get; set; }
}

public sealed class IntegracionConfiguracionDto
{
    public int Id { get; set; }
    public string NombreEmpresa { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public bool Estado { get; set; }
}

public sealed class IntegracionConfiguracionGuardarDto
{
    public string NombreEmpresa { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ApiKey { get; set; }
    public bool Estado { get; set; } = true;
}

public sealed class IntegracionAuditoriaDto
{
    public long Id { get; set; }
    public int? IntegracionEmpresaId { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public string Operacion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public long? DuracionMs { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int RegistrosEnviados { get; set; }
    public string? Detalle { get; set; }
    public string? IpOrigen { get; set; }
}
