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

	public AuditoriaPageDto Listar(int page, int pageSize, string? search, string? sortBy, string? sortDirection)
	{
		page = Math.Max(page, 1);
		pageSize = Math.Clamp(pageSize, 1, 100);
		var offset = (page - 1) * pageSize;
		var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
		var orderColumn = sortBy?.ToLowerInvariant() switch
		{
			"auditid" => "AuditId",
			"changedat" => "ChangedAt",
			"tablename" => "TableName",
			"recordkey" => "RecordKey",
			"actiontype" => "ActionType",
			"changedby" => "ChangedBy",
			"operationname" => "OperationName",
			_ => "AuditId"
		};
		var orderDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
		var tieBreaker = orderColumn == "AuditId" ? string.Empty : ", AuditId DESC";
		const string where = """
			WHERE @Search IS NULL OR
			      TableName LIKE @SearchPattern OR RecordKey LIKE @SearchPattern OR
			      ActionType LIKE @SearchPattern OR ChangedBy LIKE @SearchPattern OR
			      OperationName LIKE @SearchPattern OR HostName LIKE @SearchPattern
			""";
		var sql = $"""
			SELECT COUNT(1) FROM dbo.AuditLog {where};

			SELECT AuditId, SchemaName, TableName, RecordKey, ActionType,
			       ChangedAt, ChangedBy, ApplicationName, OperationName,
			       HostName, TransactionId, OldValues, NewValues
			FROM dbo.AuditLog
			{where}
			ORDER BY {orderColumn} {orderDirection}{tieBreaker}
			OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
			""";

		var lista = new List<AuditoriaDto>();
		using var con = _conexion.ObtenerConexion();
		con.Open();
		using var cmd = new SqlCommand(sql, con);
		var searchParameter = cmd.Parameters.Add("@Search", System.Data.SqlDbType.NVarChar, 256);
		searchParameter.Value = (object?)normalizedSearch ?? DBNull.Value;
		var searchPatternParameter = cmd.Parameters.Add("@SearchPattern", System.Data.SqlDbType.NVarChar, 512);
		searchPatternParameter.Value = (object?)($"%{normalizedSearch}%") ?? DBNull.Value;
		cmd.Parameters.AddWithValue("@Offset", offset);
		cmd.Parameters.AddWithValue("@PageSize", pageSize);
		using var dr = cmd.ExecuteReader();
		dr.Read();
		var totalItems = dr.GetInt32(0);
		dr.NextResult();

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

		return new AuditoriaPageDto
		{
			Items = lista,
			Page = page,
			PageSize = pageSize,
			TotalItems = totalItems,
			TotalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize))
		};
	}

	private static string? ToNullableString(object value) =>
		value == DBNull.Value ? null : Convert.ToString(value);
}
