//using ApiLinaAgbd.Data;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();
//builder.Services.AddSingleton<Conexion>();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//	app.UseSwagger();
//	app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Facturacion;
using ApiLinaAgbd.Services;
using ApiLinaAgbd.Services.Facturacion;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<Conexion>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<CloudinaryService>();

builder.Services.Configure<FacturacionSettings>(
	builder.Configuration.GetSection(FacturacionSettings.SectionName));

builder.Services.AddHttpClient<FacturacionSunatService>((sp, client) =>
{
	var settings = sp.GetRequiredService<IOptions<FacturacionSettings>>().Value;
	var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
		? "https://back.apisunat.com/"
		: settings.BaseUrl.TrimEnd('/') + "/";

	client.BaseAddress = new Uri(baseUrl);
	client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<BoletaUblBuilder>();
builder.Services.AddScoped<BoletaService>();
builder.Services.AddScoped<ComprobanteVentasService>();
builder.Services.AddScoped<FacturaUblBuilder>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<NotaDebitoUblBuilder>();
builder.Services.AddScoped<NotaDebitoService>();

// Configuración de CORS
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

// Activar CORS
app.UseCors("ReactPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
