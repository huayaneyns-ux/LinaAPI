using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ApiLinaAgbd.Services.Facturacion.Shared
{
	public sealed class FacturacionPdfLocalService
	{
		private readonly IWebHostEnvironment _environment;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly FacturacionSunatService _facturacionSunatService;
		private readonly ILogger<FacturacionPdfLocalService> _logger;

		private static readonly (string Format, string FileName)[] PdfFormats =
		[
			("A4", "A4.pdf"),
			("A5", "A5.pdf"),
			("58mm", "58mm.pdf"),
			("80mm", "80mm.pdf"),
		];

		public FacturacionPdfLocalService(
			IWebHostEnvironment environment,
			IHttpContextAccessor httpContextAccessor,
			FacturacionSunatService facturacionSunatService,
			ILogger<FacturacionPdfLocalService> logger)
		{
			_environment = environment;
			_httpContextAccessor = httpContextAccessor;
			_facturacionSunatService = facturacionSunatService;
			_logger = logger;
		}

		public async Task<(string? A4, string? A5, string? Ticket58, string? Ticket80)> GuardarDesdeUrlsAsync(
			Guid voucherId,
			(string? A4, string? A5, string? Ticket58, string? Ticket80) urls)
		{
			var resultados = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

			foreach (var (format, fileName) in PdfFormats)
			{
				var sourceUrl = format switch
				{
					"A4" => urls.A4,
					"A5" => urls.A5,
					"58mm" => urls.Ticket58,
					"80mm" => urls.Ticket80,
					_ => null
				};

				if (string.IsNullOrWhiteSpace(sourceUrl))
				{
					resultados[format] = null;
					continue;
				}

				try
				{
					var contenido = await _facturacionSunatService.DescargarContenidoAsync(sourceUrl);
					var rutaArchivo = ObtenerRutaArchivo(voucherId, fileName);
					Directory.CreateDirectory(Path.GetDirectoryName(rutaArchivo)!);
					await File.WriteAllBytesAsync(rutaArchivo, contenido.Content);
					resultados[format] = ObtenerUrlPublica(voucherId, fileName);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "No se pudo guardar el PDF {Format} del voucher {VoucherId}.", format, voucherId);
					resultados[format] = null;
				}
			}

			return (
				resultados.TryGetValue("A4", out var a4) ? a4 : null,
				resultados.TryGetValue("A5", out var a5) ? a5 : null,
				resultados.TryGetValue("58mm", out var ticket58) ? ticket58 : null,
				resultados.TryGetValue("80mm", out var ticket80) ? ticket80 : null
			);
		}

		public async Task<(byte[] Content, string ContentType, string FileName)> LeerPdfLocalAsync(string? storedUrl, string downloadFileName)
		{
			var rutaArchivo = ResolverRutaLocal(storedUrl);
			if (rutaArchivo is null || !File.Exists(rutaArchivo))
			{
				throw new InvalidOperationException("El PDF local no existe para este comprobante.");
			}

			var content = await File.ReadAllBytesAsync(rutaArchivo);
			return (content, "application/pdf", downloadFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? downloadFileName : $"{downloadFileName}.pdf");
		}

		private string ObtenerUrlPublica(Guid voucherId, string fileName)
		{
			var relativePath = $"/facturacion/{voucherId:D}/{fileName}";
			var request = _httpContextAccessor.HttpContext?.Request;
			if (request is null)
			{
				return relativePath;
			}

			return $"{request.Scheme}://{request.Host}{relativePath}";
		}

		private string ObtenerRutaArchivo(Guid voucherId, string fileName)
		{
			return Path.Combine(_environment.ContentRootPath, "wwwroot", "facturacion", voucherId.ToString("D"), fileName);
		}

		private string? ResolverRutaLocal(string? storedUrl)
		{
			if (string.IsNullOrWhiteSpace(storedUrl))
			{
				return null;
			}

			string path;
			if (Uri.TryCreate(storedUrl, UriKind.Absolute, out var uri))
			{
				path = uri.AbsolutePath;
			}
			else
			{
				path = storedUrl;
			}

			path = WebUtility.UrlDecode(path).Replace('\\', '/');
			const string prefix = "/facturacion/";
			var index = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
			if (index < 0)
			{
				return null;
			}

			var relative = path.Substring(index + 1).TrimStart('/');
			return Path.Combine(_environment.ContentRootPath, "wwwroot", relative.Replace('/', Path.DirectorySeparatorChar));
		}
	}
}
