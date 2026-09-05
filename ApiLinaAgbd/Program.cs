using ApiLinaAgbd;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.ApiPeru;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Security;
using ApiLinaAgbd.Swagger;
using ApiLinaAgbd.Repositories.Compras.Compra;
using ApiLinaAgbd.Repositories.Compras.Proveedor;
using ApiLinaAgbd.Repositories.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Repositories.Facturacion.Documentos;
using ApiLinaAgbd.Repositories.Facturacion.LiquidacionCompra;
using ApiLinaAgbd.Repositories.Facturacion.NotaCredito;
using ApiLinaAgbd.Repositories.Facturacion.NotaDebito;
using ApiLinaAgbd.Repositories.Inventario.Categoria;
using ApiLinaAgbd.Repositories.Inventario.Lote;
using ApiLinaAgbd.Repositories.Inventario.Marca;
using ApiLinaAgbd.Repositories.Inventario.Producto;
using ApiLinaAgbd.Repositories.Inventario.UnidadMedida;
using ApiLinaAgbd.Repositories.MetodoPago;
using ApiLinaAgbd.Repositories.Persona;
using ApiLinaAgbd.Repositories.Seguridad.Auth;
using ApiLinaAgbd.Repositories.Seguridad.Rol;
using ApiLinaAgbd.Repositories.Seguridad.Usuario;
using ApiLinaAgbd.Repositories.Ventas.Caja;
using ApiLinaAgbd.Repositories.Ventas.Lugares;
using ApiLinaAgbd.Repositories.Ventas.PedidosRecibidos;
using ApiLinaAgbd.Repositories.Ventas.VentaRealizada;
using ApiLinaAgbd.Services.Compras.Compra;
using ApiLinaAgbd.Services.Compras.Proveedor;
using ApiLinaAgbd.Services.Facturacion.ComprobantesVenta;
using ApiLinaAgbd.Services.Facturacion.Documentos;
using ApiLinaAgbd.Services.Facturacion.LiquidacionCompra;
using ApiLinaAgbd.Services.Facturacion.NotaCredito;
using ApiLinaAgbd.Services.Facturacion.NotaDebito;
using ApiLinaAgbd.Services.Facturacion.Shared;
using ApiLinaAgbd.Services.Imagen;
using ApiLinaAgbd.Services.Inventario.Categoria;
using ApiLinaAgbd.Services.Inventario.Lote;
using ApiLinaAgbd.Services.Inventario.Marca;
using ApiLinaAgbd.Services.Inventario.Producto;
using ApiLinaAgbd.Services.Inventario.UnidadMedida;
using ApiLinaAgbd.Services.MetodoPago;
using ApiLinaAgbd.Services.Persona;
using ApiLinaAgbd.Services.Seguridad.Auth;
using ApiLinaAgbd.Services.Seguridad.Rol;
using ApiLinaAgbd.Services.Seguridad.Usuario;
using ApiLinaAgbd.Services.Ventas.Caja;
using ApiLinaAgbd.Services.Ventas.Lugares;
using ApiLinaAgbd.Services.Ventas.PedidosRecibidos;
using ApiLinaAgbd.Services.Ventas.VentaRealizada;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

// Keys desde archivo .env (incluye API_AUTH_KEY=UNFV_FIIS2026)
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<Conexion>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Api Lina AGBD",
		Version = "v1",
		Description = "API Lina — Facturación electrónica y módulos operativos. Use Authorize e ingrese la API_AUTH_KEY del archivo .env."
	});
	options.DocumentFilter<FacturacionTagsDocumentFilter>();

	// Candado / ranura Authorize en Swagger
	options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
	{
		Type = SecuritySchemeType.ApiKey,
		Name = ApiKeyMiddleware.HeaderName,
		In = ParameterLocation.Header,
		Description = "Clave del archivo .env (API_AUTH_KEY). Ejemplo: UNFV_FIIS2026"
	});
	options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
	{
		[new OpenApiSecuritySchemeReference("ApiKey", document)] = []
	});
});
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<FacturacionSettings>(
	builder.Configuration.GetSection(FacturacionSettings.SectionName));

builder.Services.Configure<ApiPeruSettings>(
	builder.Configuration.GetSection(ApiPeruSettings.SectionName));

// Facturacion - SUNAT HTTP client
builder.Services.AddHttpClient<FacturacionSunatService>((sp, client) =>
{
	var settings = sp.GetRequiredService<IOptions<FacturacionSettings>>().Value;
	var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
		? "https://back.apisunat.com/"
		: settings.BaseUrl.TrimEnd('/') + "/";

	client.BaseAddress = new Uri(baseUrl);
	client.Timeout = TimeSpan.FromSeconds(60);
});

// Facturacion - builders / shared
builder.Services.AddScoped<BoletaUblBuilder>();
builder.Services.AddScoped<FacturaUblBuilder>();
builder.Services.AddScoped<FacturacionPdfLocalService>();
builder.Services.AddScoped<LiquidacionCompraUblBuilder>();
builder.Services.AddScoped<NotaCreditoUblBuilder>();
builder.Services.AddScoped<NotaDebitoUblBuilder>();

// Facturacion - repositories + services
builder.Services.AddScoped<IComprobanteVentasRepository, ComprobanteVentasRepository>();
builder.Services.AddScoped<IDocumentoFacturacionRepository, DocumentoFacturacionRepository>();
builder.Services.AddScoped<ILiquidacionCompraRepository, LiquidacionCompraRepository>();
builder.Services.AddScoped<INotaCreditoRepository, NotaCreditoRepository>();
builder.Services.AddScoped<INotaDebitoRepository, NotaDebitoRepository>();
builder.Services.AddScoped<IComprobanteVentasService, ComprobanteVentasService>();
builder.Services.AddScoped<IDocumentoFacturacionService, DocumentoFacturacionService>();
builder.Services.AddScoped<ILiquidacionCompraService, LiquidacionCompraService>();
builder.Services.AddScoped<INotaCreditoService, NotaCreditoService>();
builder.Services.AddScoped<INotaDebitoService, NotaDebitoService>();

// Inventario
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IMarcaRepository, MarcaRepository>();
builder.Services.AddScoped<IMarcaService, MarcaService>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IUnidadMedidaRepository, UnidadMedidaRepository>();
builder.Services.AddScoped<IUnidadMedidaService, UnidadMedidaService>();
builder.Services.AddScoped<ILoteRepository, LoteRepository>();
builder.Services.AddScoped<ILoteService, LoteService>();

// Compras
builder.Services.AddScoped<ICompraRepository, CompraRepository>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();

// Ventas
builder.Services.AddScoped<ICajaRepository, CajaRepository>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<ILugaresRepository, LugaresRepository>();
builder.Services.AddScoped<ILugaresService, LugaresService>();
builder.Services.AddScoped<IPedidosRecibidosRepository, PedidosRecibidosRepository>();
builder.Services.AddScoped<IPedidosRecibidosService, PedidosRecibidosService>();
builder.Services.AddScoped<IVentaRealizadaRepository, VentaRealizadaRepository>();
builder.Services.AddScoped<IVentaRealizadaService, VentaRealizadaService>();

// Seguridad
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// MetodoPago
builder.Services.AddScoped<IMetodoPagoRepository, MetodoPagoRepository>();
builder.Services.AddScoped<IMetodoPagoService, MetodoPagoService>();

// Imagen
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Persona / ApiPeru
builder.Services.AddScoped<IPersonaRepository, PersonaRepository>();
builder.Services.AddHttpClient<IApiPeruService, ApiPeruService>((sp, client) =>
{
	var settings = sp.GetRequiredService<IOptions<ApiPeruSettings>>().Value;
	var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
		? "https://api.apiperu.pe/"
		: settings.BaseUrl.TrimEnd('/') + "/";

	client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("ReactPolicy", policy =>
	{
		policy.WithOrigins("http://localhost:5173")
			  .AllowAnyHeader()
			  .AllowAnyMethod();
	});
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("ReactPolicy");
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
