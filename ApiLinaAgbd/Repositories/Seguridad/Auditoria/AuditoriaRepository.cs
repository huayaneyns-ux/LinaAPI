using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Seguridad;

namespace ApiLinaAgbd.Repositories.Seguridad.Auditoria;

public sealed class AuditoriaRepository : IAuditoriaRepository
{
	private readonly Conexion _conexion;

	public AuditoriaRepository(Conexion conexion)
	{
		_conexion = conexion;
	}

	public List<AuditoriaDto> Listar()
	{
		const string sql = """
			SELECT AuditId, SchemaName, TableName, RecordKey, ActionType,
			       ChangedAt, ChangedBy, ApplicationName, OperationName,
			       HostName, TransactionId, OldValues, NewValues
			FROM dbo.AuditLog
			ORDER BY AuditId DESC;
			""";

		var lista = new List<AuditoriaDto>();
		using var con = _conexion.ObtenerConexion();
		con.Open();
		using var cmd = new SqlCommand(sql, con);
		using var dr = cmd.ExecuteReader();

		while (dr.Read())
		{
			lista.Add(new AuditoriaDto
			{
				AuditId = Convert.ToInt64(dr["AuditId"]),
				SchemaName = Convert.ToString(dr["SchemaName"]) ?? string.Empty,
				TableName = Convert.ToString(dr["TableName"]) ?? string.Empty,
				RecordKey = Convert.ToString(dr["RecordKey"]) ?? string.Empty,
				ActionType = Convert.ToString(dr["ActionType"]) ?? string.Empty,
				ChangedAt = Convert.ToDateTime(dr["ChangedAt"]),
				ChangedBy = ToNullableString(dr["ChangedBy"]),
				ApplicationName = ToNullableString(dr["ApplicationName"]),
				OperationName = ToNullableString(dr["OperationName"]),
				HostName = ToNullableString(dr["HostName"]),
				TransactionId = dr["TransactionId"] == DBNull.Value ? null : Convert.ToInt64(dr["TransactionId"]),
				OldValues = ToNullableString(dr["OldValues"]),
				NewValues = ToNullableString(dr["NewValues"]),
			});
		}

		return lista;
	}

	private static string? ToNullableString(object value) =>
		value == DBNull.Value ? null : Convert.ToString(value);
}
