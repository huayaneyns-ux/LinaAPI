namespace ApiLinaAgbd.Models.Facturacion
{
	public class FacturacionSettings
	{
		public const string SectionName = "FacturacionSettings";

		public string BaseUrl { get; set; } = string.Empty;

		public string PersonaId { get; set; } = string.Empty;

		public string PersonaToken { get; set; } = string.Empty;

		public string SendBillPath { get; set; } = "personas/v1/sendBill";

		public EmisorSettings Emisor { get; set; } = new();
	}

	public class EmisorSettings
	{
		public string Ruc { get; set; } = string.Empty;

		public string TipoDocumento { get; set; } = "6";

		public string NombreComercial { get; set; } = string.Empty;

		public string RazonSocial { get; set; } = string.Empty;

		public string Direccion { get; set; } = string.Empty;

		public string CodigoEstablecimiento { get; set; } = "0000";
	}
}
