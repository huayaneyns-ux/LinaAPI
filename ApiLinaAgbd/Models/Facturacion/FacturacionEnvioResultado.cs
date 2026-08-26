namespace ApiLinaAgbd.Models.Facturacion
{
	public class FacturacionEnvioResultado
	{
		public bool Exitoso { get; set; }

		public int StatusCode { get; set; }

		public string Mensaje { get; set; } = string.Empty;

		public string? FileName { get; set; }

		public string? DetalleError { get; set; }

		public object? RespuestaApi { get; set; }
	}
}
