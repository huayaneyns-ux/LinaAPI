using System.Text.Json.Serialization;

namespace ApiLinaAgbd.Models.Facturacion.Ubl
{
	public class SendBillRequest
	{
		[JsonPropertyName("personaId")]
		public string PersonaId { get; set; } = string.Empty;

		[JsonPropertyName("personaToken")]
		public string PersonaToken { get; set; } = string.Empty;

		[JsonPropertyName("fileName")]
		public string FileName { get; set; } = string.Empty;

		[JsonPropertyName("documentBody")]
		public object DocumentBody { get; set; } = new();
	}
}
