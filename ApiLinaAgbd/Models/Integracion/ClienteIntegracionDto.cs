namespace ApiLinaAgbd.Models.Integracion;

public class ClienteIntegracionDto
{
	public int? id { get; set; }
	public string nombre { get; set; } = string.Empty;
	public string documento { get; set; } = string.Empty;
	public string telefono { get; set; } = string.Empty;
	public string email { get; set; } = string.Empty;
}

public class ClienteWebhookDto
{
	public string nombre { get; set; } = string.Empty;
	public string documento { get; set; } = string.Empty;
	public string telefono { get; set; } = string.Empty;
	public string email { get; set; } = string.Empty;
}
