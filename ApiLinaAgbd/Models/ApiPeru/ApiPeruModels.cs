using System.Text.Json.Serialization;

namespace ApiLinaAgbd.Models.ApiPeru
{
	public class ApiPeruSettings
	{
		public const string SectionName = "ApiPeru";

		public string BaseUrl { get; set; } = "https://api.apiperu.pe/";
		public string Token { get; set; } = string.Empty;
	}

	public class ConsultaPersonaRequestDto
	{
		public string TipoDocumento { get; set; } = string.Empty;
		public string Numero { get; set; } = string.Empty;
	}

	public class PersonaResponseDto
	{
		public bool Success { get; set; }
		public string Mensaje { get; set; } = string.Empty;
		public string? Numero { get; set; }
		public string? Nombre { get; set; }
		public string? Origen { get; set; }
	}

	public class ApiPeruDniResponse
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("message")]
		public string? Message { get; set; }

		[JsonPropertyName("data")]
		public ApiPeruDniData? Data { get; set; }
	}

	public class ApiPeruDniData
	{
		[JsonPropertyName("numero")]
		public string? Numero { get; set; }

		[JsonPropertyName("nombre_completo")]
		public string? NombreCompleto { get; set; }

		[JsonPropertyName("nombres")]
		public string? Nombres { get; set; }

		[JsonPropertyName("apellido_paterno")]
		public string? ApellidoPaterno { get; set; }

		[JsonPropertyName("apellido_materno")]
		public string? ApellidoMaterno { get; set; }

		[JsonPropertyName("codigo_verificacion")]
		public object? CodigoVerificacion { get; set; }

		[JsonPropertyName("direccion")]
		public string? Direccion { get; set; }

		[JsonPropertyName("direccion_completa")]
		public string? DireccionCompleta { get; set; }

		[JsonPropertyName("ubigeo_reniec")]
		public string? UbigeoReniec { get; set; }

		[JsonPropertyName("ubigeo_sunat")]
		public string? UbigeoSunat { get; set; }
	}

	public class ApiPeruRucResponse
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }

		[JsonPropertyName("message")]
		public string? Message { get; set; }

		[JsonPropertyName("data")]
		public ApiPeruRucData? Data { get; set; }
	}

	public class ApiPeruRucData
	{
		[JsonPropertyName("ruc")]
		public string? Ruc { get; set; }

		[JsonPropertyName("nombre_o_razon_social")]
		public string? NombreORazonSocial { get; set; }

		[JsonPropertyName("estado")]
		public string? Estado { get; set; }

		[JsonPropertyName("condicion")]
		public string? Condicion { get; set; }

		[JsonPropertyName("departamento")]
		public string? Departamento { get; set; }

		[JsonPropertyName("provincia")]
		public string? Provincia { get; set; }

		[JsonPropertyName("distrito")]
		public string? Distrito { get; set; }

		[JsonPropertyName("direccion")]
		public string? Direccion { get; set; }

		[JsonPropertyName("direccion_completa")]
		public string? DireccionCompleta { get; set; }

		[JsonPropertyName("ubigeo_sunat")]
		public string? UbigeoSunat { get; set; }
	}
}
