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
                   p.precio_venta AS InternalProductPrice, COALESCE(latestCost.Cost, latestLot.Cost) AS InternalProductCost,
                   pm.ScrapedProductId, s.Name AS Store,
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
            OUTER APPLY (
                SELECT TOP 1
                       CAST(CASE WHEN dc.cantidad > 0 THEN dc.costo_total / dc.cantidad ELSE 0 END AS decimal(18, 2)) AS Cost
                FROM dbo.detallecompra dc
                INNER JOIN dbo.compra c ON c.id = dc.id_compra
                WHERE dc.id_producto = p.id
                ORDER BY c.fecha_compra DESC, dc.id DESC
            ) latestCost
            OUTER APPLY (
                SELECT TOP 1 CAST(l.costo_unitario AS decimal(18, 2)) AS Cost
                FROM dbo.lote l
                WHERE l.id_producto = p.id
                ORDER BY l.fecha_ingreso DESC, l.id DESC
            ) latestLot
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
                InternalProductCost = reader["InternalProductCost"] == DBNull.Value ? null : Convert.ToDecimal(reader["InternalProductCost"]),
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

    public List<ScrapedProductOptionDto> ListarProductosScrapeados()
    {
        const string sql = """
            SELECT sp.Id AS ScrapedProductId, s.Name AS Store, sp.OriginalName AS Name,
                   COALESCE(latest.CurrentPrice, 0) AS Price, sp.Url,
                   active.Id AS ActiveMatchId, active.ProductoId AS MatchedProductId,
                   p.nombre AS MatchedProductName
            FROM dbo.ScrapedProduct sp
            INNER JOIN dbo.Store s ON s.Id = sp.StoreId
            OUTER APPLY (
                SELECT TOP 1 pph.CurrentPrice
                FROM dbo.ProductPriceHistory pph
                WHERE pph.ScrapedProductId = sp.Id
                ORDER BY pph.CapturedAt DESC, pph.Id DESC
            ) latest
            OUTER APPLY (
                SELECT TOP 1 pm.Id, pm.ProductoId
                FROM dbo.ProductMatch pm
                WHERE pm.ScrapedProductId = sp.Id AND pm.IsActive = 1
                ORDER BY pm.Id DESC
            ) active
            LEFT JOIN dbo.producto p ON p.id = active.ProductoId
            ORDER BY s.Name, sp.OriginalName;
            """;

        var result = new List<ScrapedProductOptionDto>();
        using var connection = _conexion.ObtenerConexion();
        connection.Open();
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ScrapedProductOptionDto
            {
                ScrapedProductId = Convert.ToInt64(reader["ScrapedProductId"]),
                Store = reader["Store"].ToString() ?? string.Empty,
                Name = reader["Name"].ToString() ?? string.Empty,
                Price = Convert.ToDecimal(reader["Price"]),
                Url = reader["Url"].ToString() ?? string.Empty,
                ActiveMatchId = reader["ActiveMatchId"] == DBNull.Value ? null : Convert.ToInt64(reader["ActiveMatchId"]),
                MatchedProductId = reader["MatchedProductId"] == DBNull.Value ? null : Convert.ToInt32(reader["MatchedProductId"]),
                MatchedProductName = reader["MatchedProductName"] == DBNull.Value ? null : reader["MatchedProductName"].ToString(),
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

    public void CrearMatchManual(ScrapingManualMatchDto match)
    {
        if (match.ScrapedProductId <= 0 || match.ProductoId <= 0)
            throw new ArgumentException("Debes seleccionar un producto de tu tienda y uno scrapeado.");

        using var connection = _conexion.ObtenerConexion();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            long storeId;
            long? activeMatchId = null;
            int? activeProductId = null;
            using (var lookup = new SqlCommand("""
                SELECT sp.StoreId, pm.Id, pm.ProductoId
                FROM dbo.ScrapedProduct sp
                LEFT JOIN dbo.ProductMatch pm ON pm.ScrapedProductId = sp.Id AND pm.IsActive = 1
                WHERE sp.Id = @ScrapedProductId;
                """, connection, transaction))
            {
                lookup.Parameters.Add("@ScrapedProductId", SqlDbType.BigInt).Value = match.ScrapedProductId;
                using var reader = lookup.ExecuteReader();
                if (!reader.Read()) throw new KeyNotFoundException("El producto scrapeado no existe.");
                storeId = Convert.ToInt64(reader["StoreId"]);
                if (reader["Id"] != DBNull.Value) activeMatchId = Convert.ToInt64(reader["Id"]);
                if (reader["ProductoId"] != DBNull.Value) activeProductId = Convert.ToInt32(reader["ProductoId"]);
            }

            if (activeProductId is not null && activeProductId != match.ProductoId)
                throw new InvalidOperationException("Ese producto scrapeado ya está relacionado con otro producto.");

            const string deactivate = """
                UPDATE pm SET IsActive = 0, ReviewedBy = @ReviewedBy, ReviewedAt = SYSUTCDATETIME()
                FROM dbo.ProductMatch pm
                INNER JOIN dbo.ScrapedProduct sp ON sp.Id = pm.ScrapedProductId
                WHERE pm.IsActive = 1 AND pm.ProductoId = @ProductoId AND sp.StoreId = @StoreId
                  AND (@CurrentMatchId IS NULL OR pm.Id <> @CurrentMatchId);
                """;
            using (var deactivateCommand = new SqlCommand(deactivate, connection, transaction))
            {
                deactivateCommand.Parameters.Add("@ReviewedBy", SqlDbType.NVarChar, 150).Value = (object?)match.ReviewedBy ?? DBNull.Value;
                deactivateCommand.Parameters.Add("@ProductoId", SqlDbType.Int).Value = match.ProductoId;
                deactivateCommand.Parameters.Add("@StoreId", SqlDbType.BigInt).Value = storeId;
                deactivateCommand.Parameters.Add("@CurrentMatchId", SqlDbType.BigInt).Value = (object?)activeMatchId ?? DBNull.Value;
                deactivateCommand.ExecuteNonQuery();
            }

            if (activeMatchId is not null)
            {
                using var update = new SqlCommand("""
                    UPDATE dbo.ProductMatch SET ProductoId = @ProductoId, Decision = 'MANUAL_MATCH',
                        ReviewedBy = @ReviewedBy, ReviewedAt = SYSUTCDATETIME()
                    WHERE Id = @Id AND IsActive = 1;
                    """, connection, transaction);
                update.Parameters.Add("@ProductoId", SqlDbType.Int).Value = match.ProductoId;
                update.Parameters.Add("@ReviewedBy", SqlDbType.NVarChar, 150).Value = (object?)match.ReviewedBy ?? DBNull.Value;
                update.Parameters.Add("@Id", SqlDbType.BigInt).Value = activeMatchId.Value;
                update.ExecuteNonQuery();
            }
            else
            {
                using var insert = new SqlCommand("""
                    INSERT INTO dbo.ProductMatch (ScrapedProductId, ProductoId, Decision, Score, IsActive, ReviewedBy, ReviewedAt)
                    VALUES (@ScrapedProductId, @ProductoId, 'MANUAL_MATCH', NULL, 1, @ReviewedBy, SYSUTCDATETIME());
                    """, connection, transaction);
                insert.Parameters.Add("@ScrapedProductId", SqlDbType.BigInt).Value = match.ScrapedProductId;
                insert.Parameters.Add("@ProductoId", SqlDbType.Int).Value = match.ProductoId;
                insert.Parameters.Add("@ReviewedBy", SqlDbType.NVarChar, 150).Value = (object?)match.ReviewedBy ?? DBNull.Value;
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }
}
