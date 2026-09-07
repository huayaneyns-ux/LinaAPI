using System.Data.SqlClient;
using ApiLinaAgbd.Data;
using ApiLinaAgbd.Models.Facturacion.Notas;

namespace ApiLinaAgbd.Repositories.Facturacion.NotaDebito
{
	public class NotaDebitoRepository : INotaDebitoRepository
	{
		private readonly Conexion _conexion;

		public NotaDebitoRepository(Conexion conexion) => _conexion = conexion;

		public SqlConnection CreateConnection() => _conexion.ObtenerConexion();

		public async Task<List<NotaComprobanteBaseDisponibleDto>> ListarComprobantesBaseAsync()
		{
			using var con = CreateConnection();
			await con.OpenAsync();
			const string sql = """
			WITH ItemsConDevoluciones AS
			(
				SELECT i.Id, i.VoucherId, i.ProductId, i.ProductCode, i.Description,
				       i.LineNumber, i.Quantity, i.UnitCode, i.UnitPrice, i.SaleValue, i.Igv,
				       i.Total,
				       COALESCE((
				           SELECT SUM(dev.Quantity)
				           FROM dbo.VoucherItem dev
				           INNER JOIN dbo.Voucher devVoucher ON devVoucher.Id = dev.VoucherId
				           INNER JOIN dbo.VoucherAdjustment devAdjustment ON devAdjustment.VoucherId = dev.VoucherId
				           WHERE devAdjustment.ReferencedVoucherId = i.VoucherId
				             AND devAdjustment.ReasonCode IN ('05', '07')
				             AND devVoucher.SunatTypeCode = '07'
				             AND devVoucher.SunatStatus = 'ACEPTADO'
				             AND dev.ReferencedVoucherItemId = i.Id
				       ), 0) AS ReturnedQuantity
				FROM dbo.VoucherItem i
			)
			SELECT v.Id, v.SunatTypeCode, v.Series, v.Number, v.IssueDate, v.Currency,
			       v.Subtotal, v.Igv, v.Total,
			       COALESCE(p.Name, '') ClienteNombre, COALESCE(p.DocumentType, '') ClienteTipoDocumento,
			       COALESCE(p.DocumentNumber, '') ClienteDocumento, COALESCE(p.Address, '') ClienteDireccion,
			       i.Id ItemId, i.ProductId, COALESCE(i.ProductCode, '') ProductCode,
			       i.Description,
			       i.Quantity - i.ReturnedQuantity AS Quantity,
			       COALESCE(i.UnitPrice, 0) UnitPrice,
			       CASE WHEN i.Quantity > 0 THEN COALESCE(i.SaleValue, 0) * (i.Quantity - i.ReturnedQuantity) / i.Quantity ELSE 0 END SaleValue,
			       CASE WHEN i.Quantity > 0 THEN COALESCE(i.Igv, 0) * (i.Quantity - i.ReturnedQuantity) / i.Quantity ELSE 0 END ItemIgv,
			       CASE WHEN i.Quantity > 0 THEN COALESCE(i.Total, 0) * (i.Quantity - i.ReturnedQuantity) / i.Quantity ELSE 0 END ItemTotal,
			       i.UnitCode
			FROM dbo.Voucher v
			LEFT JOIN dbo.VoucherParty p ON p.VoucherId = v.Id AND p.Role = 'CUSTOMER'
			LEFT JOIN ItemsConDevoluciones i ON i.VoucherId = v.Id
			WHERE v.SunatTypeCode IN ('01','03') AND v.SunatStatus = 'ACEPTADO'
			  AND NOT EXISTS (
			      SELECT 1 FROM dbo.VoucherAdjustment a
			      INNER JOIN dbo.Voucher n ON n.Id = a.VoucherId
			      WHERE a.ReferencedVoucherId = v.Id AND n.SunatTypeCode = '07'
			        AND n.SunatStatus = 'ACEPTADO' AND a.ReasonCode IN ('01','02','06')
			  )
			  AND EXISTS (
			      SELECT 1
			      FROM ItemsConDevoluciones disponible
			      WHERE disponible.VoucherId = v.Id
			        AND disponible.Quantity - disponible.ReturnedQuantity > 0
			  )
			  AND (i.Id IS NULL OR i.Quantity - i.ReturnedQuantity > 0)
			ORDER BY v.CreatedAt DESC, i.LineNumber;
			""";
			using var cmd = new SqlCommand(sql, con);
			using var dr = await cmd.ExecuteReaderAsync();
			var result = new Dictionary<string, NotaComprobanteBaseDisponibleDto>(StringComparer.OrdinalIgnoreCase);
			while (await dr.ReadAsync())
			{
				var id = dr["Id"].ToString()!;
				if (!result.TryGetValue(id, out var voucher))
				{
					var code = dr["SunatTypeCode"].ToString()!;
					voucher = new NotaComprobanteBaseDisponibleDto
					{
						Id = id, Tipo = code == "01" ? "FACTURA" : "BOLETA", SunatTypeCode = code,
						Serie = dr["Series"].ToString()!, Numero = dr["Number"].ToString()!,
						FechaEmision = Convert.ToDateTime(dr["IssueDate"]).ToString("yyyy-MM-dd"),
						Moneda = dr["Currency"].ToString()!, ClienteNombre = dr["ClienteNombre"].ToString()!,
						ClienteTipoDocumento = dr["ClienteTipoDocumento"].ToString()!, ClienteDocumento = dr["ClienteDocumento"].ToString()!,
						ClienteDireccion = dr["ClienteDireccion"].ToString()!, Subtotal = dr["Subtotal"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Subtotal"]),
						Igv = dr["Igv"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Igv"]), Total = dr["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Total"])
					};
					result[id] = voucher;
				}
				if (dr["ItemId"] != DBNull.Value)
				{
					var quantity = Convert.ToDecimal(dr["Quantity"]);
					var sale = Convert.ToDecimal(dr["SaleValue"]);
					voucher.Items.Add(new NotaComprobanteBaseItemDto
					{
						Id = dr["ItemId"].ToString()!, ProductoId = dr["ProductId"] == DBNull.Value ? null : Convert.ToInt32(dr["ProductId"]),
						Codigo = dr["ProductCode"].ToString()!, Descripcion = dr["Description"].ToString()!, Cantidad = quantity,
						PrecioUnitario = dr["UnitPrice"] == DBNull.Value ? (quantity > 0 ? sale / quantity : 0) : Convert.ToDecimal(dr["UnitPrice"]),
						ValorVenta = sale, Igv = Convert.ToDecimal(dr["ItemIgv"]), Importe = Convert.ToDecimal(dr["ItemTotal"]), UnidadMedida = dr["UnitCode"].ToString()!
					});
				}
			}
			return result.Values.ToList();
		}
	}
}
