namespace ApiLinaAgbd.Models.Integracion;

public sealed class IntegracionProductoAdminDto
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string? Sku { get; set; }
    public string? Nombre { get; set; }
    public string? EmpresaOrigen { get; set; }
    public bool Seleccionado { get; set; }
    public bool Entregado { get; set; }
}

public sealed class IntegracionProveedorAdminDto
{
    public int Id { get; set; }
    public string? Ruc { get; set; }
    public string? RazonSocial { get; set; }
    public string? EmpresaOrigen { get; set; }
    public bool Seleccionado { get; set; }
    public bool Entregado { get; set; }
}

public sealed class IntegracionCatalogoAdminDto
{
    public List<IntegracionProductoAdminDto> Productos { get; set; } = new();
    public List<IntegracionProveedorAdminDto> Proveedores { get; set; } = new();
}

public sealed class IntegracionCatalogoSeleccionGuardarDto
{
    public List<int> ProductoIds { get; set; } = new();
    public List<int> ProveedorIds { get; set; } = new();
}
