using System.Data;
using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Scraping;

namespace ApiLinaAgbd.Repositories.Scraping;

public class ScrapingRepository : IScrapingRepository
{
    private readonly Conexion _conexion;
    public ScrapingRepository(Conexion conexion) => _conexion = conexion;

    public List<ScrapingMatchDto> ListarMatches()
    {
        const string sql = """
            SELECT pm.Id, pm.ProductoId, p.nombre AS InternalProductName, p.sku AS InternalProductSku,
                   p.precio_venta AS InternalProductPrice, pm.ScrapedProductId, s.Name AS Store,
                   sp.OriginalName AS Name, COALESCE(latest.CurrentPrice, 0) AS Price, sp.Url, pm.Score,
                   pm.Decision, sp.ScrapedAt
            FROM dbo.ProductMatch pm
            INNER JOIN dbo.ScrapedProduct sp ON sp.Id = pm.ScrapedProductId
            INNER JOIN dbo.Store s ON s.Id = sp.StoreId
            LEFT JOIN dbo.producto p ON p.id = pm.ProductoId
            OUTER APPLY (
                SELECT TOP 1 pph.CurrentPrice
                FROM dbo.ProductPriceHistory pph
                WHERE pph.ScrapedProductId = sp.Id
                ORDER BY pph.CapturedAt DESC, pph.Id DESC
            ) latest
            WHERE pm.IsActive = 1 AND pm.Decision IN ('AUTO_MATCH', 'REVIEW', 'MANUAL_MATCH')
            ORDER BY pm.Decision, p.nombre, s.Name, sp.OriginalName;
            """;

        var result = new List<ScrapingMatchDto>();
        using var connection = _conexion.ObtenerConexion();
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ScrapingMatchDto
            {
                Id = Convert.ToInt64(reader["Id"]),
                InternalProductId = reader["ProductoId"] == DBNull.Value ? null : Convert.ToInt32(reader["ProductoId"]),
                InternalProductName = reader["InternalProductName"] == DBNull.Value ? null : reader["InternalProductName"].ToString(),
                InternalProductSku = reader["InternalProductSku"] == DBNull.Value ? null : reader["InternalProductSku"].ToString(),
                InternalProductPrice = reader["InternalProductPrice"] == DBNull.Value ? null : Convert.ToDecimal(reader["InternalProductPrice"]),
                ScrapedProductId = Convert.ToInt64(reader["ScrapedProductId"]),
                Store = reader["Store"].ToString() ?? string.Empty,
                Name = reader["Name"].ToString() ?? string.Empty,
                Price = Convert.ToDecimal(reader["Price"]),
                Url = reader["Url"].ToString() ?? string.Empty,
                Score = reader["Score"] == DBNull.Value ? null : Convert.ToDecimal(reader["Score"]),
                Decision = reader["Decision"].ToString() ?? string.Empty,
                ScrapedAt = Convert.ToDateTime(reader["ScrapedAt"]),
            });
        }
        return result;
    }

    public void ActualizarDecision(long matchId, ScrapingDecisionDto decision)
    {
        if (decision.Decision is not ("MANUAL_MATCH" or "NO_MATCH"))
            throw new ArgumentException("La decisión debe ser MANUAL_MATCH o NO_MATCH.");
        if (decision.Decision == "MANUAL_MATCH" && decision.ProductoId is null)
            throw new ArgumentException("ProductoId es obligatorio para MANUAL_MATCH.");

        using var connection = _conexion.ObtenerConexion();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            long storeId;
            using (var lookup = new SqlCommand("SELECT sp.StoreId FROM dbo.ProductMatch pm INNER JOIN dbo.ScrapedProduct sp ON sp.Id = pm.ScrapedProductId WHERE pm.Id = @Id AND pm.IsActive = 1", connection, transaction))
            {
                lookup.Parameters.Add("@Id", SqlDbType.BigInt).Value = matchId;
                var value = lookup.ExecuteScalar();
                if (value is null) throw new KeyNotFoundException("La coincidencia no existe o ya fue procesada.");
                storeId = Convert.ToInt64(value);
            }

            if (decision.Decision == "MANUAL_MATCH")
            {
                const string deactivate = """
                    UPDATE pm SET pm.IsActive = 0, pm.ReviewedBy = @ReviewedBy, pm.ReviewedAt = SYSUTCDATETIME()
                    FROM dbo.ProductMatch pm INNER JOIN dbo.ScrapedProduct sp ON sp.Id = pm.ScrapedProductId
                    WHERE pm.IsActive = 1 AND pm.ProductoId = @ProductoId AND sp.StoreId = @StoreId AND pm.Id <> @Id;
                    """;
                using var deactivateCommand = new SqlCommand(deactivate, connection, transaction);
                deactivateCommand.Parameters.Add("@ReviewedBy", SqlDbType.NVarChar, 150).Value = (object?)decision.ReviewedBy ?? DBNull.Value;
                deactivateCommand.Parameters.Add("@ProductoId", SqlDbType.Int).Value = decision.ProductoId!.Value;
                deactivateCommand.Parameters.Add("@StoreId", SqlDbType.BigInt).Value = storeId;
                deactivateCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = matchId;
                deactivateCommand.ExecuteNonQuery();
            }

            const string update = "UPDATE dbo.ProductMatch SET ProductoId = @ProductoId, Decision = @Decision, ReviewedBy = @ReviewedBy, ReviewedAt = SYSUTCDATETIME() WHERE Id = @Id AND IsActive = 1";
            using var updateCommand = new SqlCommand(update, connection, transaction);
            updateCommand.Parameters.Add("@ProductoId", SqlDbType.Int).Value = (object?)decision.ProductoId ?? DBNull.Value;
            updateCommand.Parameters.Add("@Decision", SqlDbType.VarChar, 20).Value = decision.Decision;
            updateCommand.Parameters.Add("@ReviewedBy", SqlDbType.NVarChar, 150).Value = (object?)decision.ReviewedBy ?? DBNull.Value;
            updateCommand.Parameters.Add("@Id", SqlDbType.BigInt).Value = matchId;
            if (updateCommand.ExecuteNonQuery() == 0) throw new KeyNotFoundException("La coincidencia ya fue procesada.");
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }
}
