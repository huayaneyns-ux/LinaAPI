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

		public string? EstadoSunat { get; set; }

		public string? CodigoRespuestaSunat { get; set; }

		public string? MensajeSunat { get; set; }

		public string? DocumentId { get; set; }

		public string? XmlUrl { get; set; }

		public string? PdfUrl { get; set; }

		public string? CdrUrl { get; set; }
	}
}
