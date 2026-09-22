using System.Security.Cryptography;
using System.Text;

namespace ApiLinaAgbd.Security
{
	/// <summary>
	/// Valida el header X-Api-Key contra API_AUTH_KEY del archivo .env
	/// </summary>
	public sealed class ApiKeyMiddleware
	{
		public const string HeaderName = "X-Api-Key";
		public const string ConfigKey = "API_AUTH_KEY";

		private static readonly PathString[] RutasPublicas =
		[
			new("/swagger"),
			new("/favicon.ico")
		];

		private readonly RequestDelegate _next;
		private readonly IConfiguration _configuration;
		private readonly string _apiKey;

		public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
		{
			_next = next;
			_configuration = configuration;
			_apiKey = configuration[ConfigKey]?.Trim() ?? string.Empty;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			if (EsRutaPublica(context.Request.Path))
			{
				await _next(context);
				return;
			}

			var esRutaIntegracion = EsRutaIntegracion(context.Request.Path);
			var claveEsperada = esRutaIntegracion
				? _configuration["INTEGRACION_API_KEY_LINA"]?.Trim() ?? string.Empty
				: _apiKey;

			if (string.IsNullOrWhiteSpace(claveEsperada))
			{
				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
				await context.Response.WriteAsJsonAsync(new
				{
					mensaje = esRutaIntegracion
						? "Falta configurar INTEGRACION_API_KEY_LINA en el archivo .env"
						: $"Falta configurar {ConfigKey} en el archivo .env"
				});
				return;
			}

			if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
				!ClavesIguales(provided.ToString(), claveEsperada))
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsJsonAsync(new
				{
					mensaje = $"No autorizado. Envíe el header {HeaderName} o use Authorize en Swagger."
				});
				return;
			}

			await _next(context);
		}

		private static bool EsRutaPublica(PathString path)
		{
			foreach (var publica in RutasPublicas)
			{
				if (path.StartsWithSegments(publica, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static bool ClavesIguales(string a, string b)
		{
			var bytesA = Encoding.UTF8.GetBytes(a);
			var bytesB = Encoding.UTF8.GetBytes(b);
			if (bytesA.Length != bytesB.Length)
			{
				return false;
			}

			return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
		}

		private static bool EsClaveValida(PathString path, string proporcionada, string apiKey, string? claveIntegracion)
		{
			if (ClavesIguales(proporcionada, apiKey)) return true;
			return (path.StartsWithSegments("/api/v1/clientes", StringComparison.OrdinalIgnoreCase) ||
				path.StartsWithSegments("/api/v1/webhooks", StringComparison.OrdinalIgnoreCase)) &&
				!string.IsNullOrWhiteSpace(claveIntegracion) && ClavesIguales(proporcionada, claveIntegracion);
		}

		private static bool EsRutaIntegracion(PathString path) =>
			path.StartsWithSegments("/api/v1/clientes", StringComparison.OrdinalIgnoreCase) ||
			path.StartsWithSegments("/api/v1/webhooks", StringComparison.OrdinalIgnoreCase);
	}
}
