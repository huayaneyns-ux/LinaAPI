namespace ApiLinaAgbd.Models.Seguridad;

public sealed class AuditoriaDto
{
	public long AuditId { get; set; }
	public string SchemaName { get; set; } = string.Empty;
	public string TableName { get; set; } = string.Empty;
	public string RecordKey { get; set; } = string.Empty;
	public string ActionType { get; set; } = string.Empty;
	public DateTime ChangedAt { get; set; }
	public string? ChangedBy { get; set; }
	public string? ApplicationName { get; set; }
	public string? OperationName { get; set; }
	public string? HostName { get; set; }
	public long? TransactionId { get; set; }
	public string? OldValues { get; set; }
	public string? NewValues { get; set; }
}
