namespace ApiLinaAgbd.Models.Integracion;

public class ClienteIntegracionDto
{
	public int? id { get; set; }
	public string nombre { get; set; } = string.Empty;
	public string? documento { get; set; }
	public string? telefono { get; set; }
	public string? email { get; set; }
	public string? origen { get; set; }
}

public class ClienteWebhookDto
{
	public string nombre { get; set; } = string.Empty;
	public string? documento { get; set; }
	public string? telefono { get; set; }
	public string? email { get; set; }
	public string origen { get; set; } = string.Empty;
}
