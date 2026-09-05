using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.Json;
using ApiLinaAgbd.Models.Facturacion;

namespace ApiLinaAgbd.Services.Facturacion.Shared
{
	internal static class FacturacionVoucherHelper
	{
		internal static DateTime ParsearFechaObligatoria(string? fechaTexto, string mensaje)
		{
			if (!DateTime.TryParse(fechaTexto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
			{
				throw new InvalidOperationException(mensaje);
			}

			return fecha.Date;
		}

		internal static decimal Redondear(decimal valor) =>
			Math.Round(valor, 2, MidpointRounding.AwayFromZero);

		internal static bool DocumentoValido(string tipoDocumento, string? numero)
		{
			var documento = (numero ?? string.Empty).Trim();
			return (tipoDocumento ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"DNI" => documento.Length == 8 && documento.All(char.IsDigit),
				"RUC" => documento.Length == 11 && documento.All(char.IsDigit),
				"CE" => documento.Length >= 6 && documento.Length <= 12,
				"PASAPORTE" => documento.Length >= 6,
				_ => false
			};
		}

		internal static string MapearTipoDocumentoSunat(string tipoDocumento, bool esFactura)
		{
			if (esFactura)
			{
				return "6";
			}

			return (tipoDocumento ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"DNI" => "1",
				"RUC" => "6",
				"CE" => "4",
				"PASAPORTE" => "7",
				_ => "-"
			};
		}

		internal static string MapearEstadoSunatUi(string? estado)
		{
			return (estado ?? string.Empty).Trim().ToUpperInvariant() switch
			{
				"ANULADO" => "ANULADO",
				"ACEPTADO" => "ACEPTADO",
				"RECHAZADO" => "RECHAZADO",
				"EXCEPCION" => "EXCEPCION",
				"OBSERVADO" => "OBSERVADO",
				"ENVIADO" => "ENVIADO",
				"NO_ENVIADO" => "PENDIENTE",
				_ => "PENDIENTE"
			};
		}

		internal static string NormalizarSunatStatusParaVoucher(FacturacionEnvioResultado envio)
		{
			var estado = string.IsNullOrWhiteSpace(envio.EstadoSunat)
				? (envio.Exitoso ? "PENDIENTE" : "EXCEPCION")
				: envio.EstadoSunat.Trim().ToUpperInvariant();

			estado = estado
				.Replace(" ", string.Empty, StringComparison.Ordinal)
				.Replace("-", string.Empty, StringComparison.Ordinal)
				.Replace("_", string.Empty, StringComparison.Ordinal);

			return estado switch
			{
				"NOENVIADO" => "NO_ENVIADO",
				"PENDIENTE" => "PENDIENTE",
				"ACEPTADO" => "ACEPTADO",
				"RECHAZADO" => "RECHAZADO",
				"EXCEPCION" => "EXCEPCION",
				"OBSERVADO" => "EXCEPCION",
				"ENVIADO" => "PENDIENTE",
				"PENDING" => "PENDIENTE",
				"SUCCESS" => "PENDIENTE",
				"ERROR" => "EXCEPCION",
				"FAILED" => "RECHAZADO",
				_ => envio.Exitoso ? "PENDIENTE" : "EXCEPCION"
			};
		}

		internal static bool FueRecibidoPorApi(FacturacionEnvioResultado resultado) =>
			resultado.Exitoso;

		internal static bool EsFalloDeComunicacion(FacturacionEnvioResultado resultado)
		{
			if (resultado.Exitoso)
			{
				return false;
			}

			if (resultado.StatusCode is 502 or 504)
			{
				return true;
			}

			var detalle = (resultado.DetalleError ?? string.Empty).ToLowerInvariant();
			return detalle.Contains("timeout", StringComparison.Ordinal) ||
				detalle.Contains("connection", StringComparison.Ordinal) ||
				detalle.Contains("conex", StringComparison.Ordinal) ||
				detalle.Contains("socket", StringComparison.Ordinal);
		}

		internal static List<string> SepararObservaciones(string? observaciones)
		{
			if (string.IsNullOrWhiteSpace(observaciones))
			{
				return new List<string>();
			}

			return observaciones
				.Split(['#', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Take(20)
				.ToList();
		}

		internal static (string? A4, string? A5, string? Ticket58, string? Ticket80) ExtraerUrlsPdf(object? respuestaApi)
		{
			if (respuestaApi is not JsonElement element || element.ValueKind != JsonValueKind.Object)
			{
				return (null, null, null, null);
			}

			if (!element.TryGetProperty("pdf", out var pdf) || pdf.ValueKind != JsonValueKind.Object)
			{
				return (null, null, null, null);
			}

			return (
				pdf.TryGetProperty("A4", out var a4) ? a4.GetString() : null,
				pdf.TryGetProperty("A5", out var a5) ? a5.GetString() : null,
				pdf.TryGetProperty("58mm", out var p58) ? p58.GetString() : null,
				pdf.TryGetProperty("80mm", out var p80) ? p80.GetString() : null
			);
		}

		internal static async Task<string> GenerarNumeroAleatorioDisponibleAsync(
			SqlConnection con,
			SqlTransaction tx,
			string tipoComprobanteSunat,
			string serie,
			string issuerRuc)
		{
			const string sql = """
				SELECT COUNT(1)
				FROM dbo.Voucher
				WHERE SunatTypeCode = @tipo
				  AND Series = @serie
				  AND IssuerRuc = @issuerRuc
				  AND Number = @number;
				""";

			for (var intento = 0; intento < 100; intento++)
			{
				var numero = Random.Shared.Next(0, 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
				using var cmd = new SqlCommand(sql, con, tx);
				cmd.Parameters.AddWithValue("@tipo", tipoComprobanteSunat);
				cmd.Parameters.AddWithValue("@serie", serie);
				cmd.Parameters.AddWithValue("@issuerRuc", issuerRuc);
				cmd.Parameters.AddWithValue("@number", numero);

				var existe = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
				if (!existe)
				{
					return numero;
				}
			}

			throw new InvalidOperationException("No se pudo generar un número aleatorio único para el comprobante.");
		}

		internal static async Task InsertarObservacionesAsync(SqlConnection con, SqlTransaction tx, Guid voucherId, string? observaciones)
		{
			var lineas = SepararObservaciones(observaciones);
			if (lineas.Count == 0)
			{
				return;
			}

			const string sql = """
				INSERT INTO dbo.VoucherObservation
				(
					Id,
					VoucherId,
					LineNumber,
					Observation
				)
				VALUES
				(
					@Id,
					@VoucherId,
					@LineNumber,
					@Observation
				);
				""";

			for (var i = 0; i < lineas.Count; i++)
			{
				using var cmd = new SqlCommand(sql, con, tx);
				cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
				cmd.Parameters.AddWithValue("@VoucherId", voucherId);
				cmd.Parameters.AddWithValue("@LineNumber", i + 1);
				cmd.Parameters.AddWithValue("@Observation", lineas[i]);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		internal static async Task InsertarPartyAsync(
			SqlConnection con,
			SqlTransaction tx,
			Guid voucherId,
			string role,
			string? documentType,
			string? documentNumber,
			string? name,
			string? address)
		{
			const string sql = """
				INSERT INTO dbo.VoucherParty
				(
					Id,
					VoucherId,
					Role,
					DocumentType,
					DocumentNumber,
					Name,
					Address
				)
				VALUES
				(
					@Id,
					@VoucherId,
					@Role,
					@DocumentType,
					@DocumentNumber,
					@Name,
					@Address
				);
				""";

			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
			cmd.Parameters.AddWithValue("@VoucherId", voucherId);
			cmd.Parameters.AddWithValue("@Role", role);
			cmd.Parameters.AddWithValue("@DocumentType", string.IsNullOrWhiteSpace(documentType) ? DBNull.Value : documentType);
			cmd.Parameters.AddWithValue("@DocumentNumber", string.IsNullOrWhiteSpace(documentNumber) ? DBNull.Value : documentNumber);
			cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? DBNull.Value : name);
			cmd.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(address) ? DBNull.Value : address);
			await cmd.ExecuteNonQueryAsync();
		}

		internal static async Task InsertarUbicacionAsync(
			SqlConnection con,
			SqlTransaction tx,
			Guid voucherId,
			string locationType,
			int districtId,
			string address,
			string? establishmentCode = null)
		{
			const string sql = """
				INSERT INTO dbo.VoucherLocation
				(
					Id,
					VoucherId,
					LocationType,
					DistrictId,
					Address,
					EstablishmentCode
				)
				VALUES
				(
					@Id,
					@VoucherId,
					@LocationType,
					@DistrictId,
					@Address,
					@EstablishmentCode
				);
				""";

			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
			cmd.Parameters.AddWithValue("@VoucherId", voucherId);
			cmd.Parameters.AddWithValue("@LocationType", locationType);
			cmd.Parameters.AddWithValue("@DistrictId", districtId);
			cmd.Parameters.AddWithValue("@Address", address);
			cmd.Parameters.AddWithValue("@EstablishmentCode", string.IsNullOrWhiteSpace(establishmentCode) ? DBNull.Value : establishmentCode);
			await cmd.ExecuteNonQueryAsync();
		}

		internal static async Task InsertarAdjustmentAsync(
			SqlConnection con,
			SqlTransaction tx,
			Guid voucherId,
			Guid referencedVoucherId,
			string reasonCode,
			string reasonDescription)
		{
			const string sql = """
				INSERT INTO dbo.VoucherAdjustment
				(
					VoucherId,
					ReferencedVoucherId,
					ReasonCode,
					ReasonDescription
				)
				VALUES
				(
					@VoucherId,
					@ReferencedVoucherId,
					@ReasonCode,
					@ReasonDescription
				);
				""";

			using var cmd = new SqlCommand(sql, con, tx);
			cmd.Parameters.AddWithValue("@VoucherId", voucherId);
			cmd.Parameters.AddWithValue("@ReferencedVoucherId", referencedVoucherId);
			cmd.Parameters.AddWithValue("@ReasonCode", reasonCode);
			cmd.Parameters.AddWithValue("@ReasonDescription", reasonDescription);
			await cmd.ExecuteNonQueryAsync();
		}

		internal static async Task RegistrarTransmisionAsync(
			SqlConnection con,
			Guid voucherId,
			string operationType,
			FacturacionEnvioResultado resultado,
			DateTime createdAtUtc,
			DateTime? respondedAtUtc = null)
		{
			var nextAttempt = await ObtenerSiguienteIntentoAsync(con, voucherId, operationType);
			var transmissionId = Guid.NewGuid();

			const string sql = """
				INSERT INTO dbo.SunatTransmission
				(
					Id,
					VoucherId,
					AttemptNumber,
					OperationType,
					TransmissionStatus,
					HttpStatus,
					SunatStatus,
					SunatDocumentId,
					ErrorMessage,
					CreatedAt,
					RespondedAt
				)
				VALUES
				(
					@Id,
					@VoucherId,
					@AttemptNumber,
					@OperationType,
					@TransmissionStatus,
					@HttpStatus,
					@SunatStatus,
					@SunatDocumentId,
					@ErrorMessage,
					@CreatedAt,
					@RespondedAt
				);
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", transmissionId);
			cmd.Parameters.AddWithValue("@VoucherId", voucherId);
			cmd.Parameters.AddWithValue("@AttemptNumber", nextAttempt);
			cmd.Parameters.AddWithValue("@OperationType", operationType);
			cmd.Parameters.AddWithValue("@TransmissionStatus", resultado.Exitoso ? "SUCCESS" : "ERROR");
			cmd.Parameters.AddWithValue("@HttpStatus", resultado.StatusCode > 0 ? resultado.StatusCode : DBNull.Value);
			cmd.Parameters.Add("@SunatStatus", SqlDbType.VarChar, 30).Value = NormalizarSunatStatusParaVoucher(resultado);
			cmd.Parameters.AddWithValue("@SunatDocumentId", (object?)resultado.DocumentId ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@ErrorMessage", (object?)ObtenerResumenTransmision(resultado) ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@CreatedAt", createdAtUtc);
			cmd.Parameters.AddWithValue("@RespondedAt", respondedAtUtc ?? DateTime.UtcNow);
			await cmd.ExecuteNonQueryAsync();
			await InsertarMensajesTransmisionAsync(con, transmissionId, resultado.RespuestaApi, "faults", "dbo.SunatTransmissionFault");
			await InsertarMensajesTransmisionAsync(con, transmissionId, resultado.RespuestaApi, "notes", "dbo.SunatTransmissionNote");
		}

		internal static async Task ActualizarVoucherPostEnvioAsync(
			SqlConnection con,
			Guid voucherId,
			FacturacionEnvioResultado envio,
			FacturacionPdfLocalService pdfLocalService)
		{
			const string sql = """
				UPDATE dbo.Voucher
				SET
					SunatStatus = CASE
						WHEN @SunatStatus IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO', 'RECHAZADO', 'EXCEPCION') THEN @SunatStatus
						ELSE 'EXCEPCION'
					END,
					SunatDocumentId = @SunatDocumentId,
					XmlUrl = @XmlUrl,
					CdrUrl = @CdrUrl,
					PdfA4Url = @PdfA4Url,
					PdfA5Url = @PdfA5Url,
					Pdf58mmUrl = @Pdf58mmUrl,
					Pdf80mmUrl = @Pdf80mmUrl,
					UpdatedAt = SYSUTCDATETIME()
				WHERE Id = @Id;
				""";

			var urlsPdf = ExtraerUrlsPdf(envio.RespuestaApi);
			var urlsPdfLocales = await pdfLocalService.GuardarDesdeUrlsAsync(voucherId, urlsPdf);
			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", voucherId);
			cmd.Parameters.Add("@SunatStatus", SqlDbType.VarChar, 30).Value = NormalizarSunatStatusParaVoucher(envio);
			cmd.Parameters.AddWithValue("@SunatDocumentId", (object?)envio.DocumentId ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@XmlUrl", (object?)envio.XmlUrl ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@CdrUrl", (object?)envio.CdrUrl ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@PdfA4Url", (object?)urlsPdfLocales.A4 ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@PdfA5Url", (object?)urlsPdfLocales.A5 ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Pdf58mmUrl", (object?)urlsPdfLocales.Ticket58 ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@Pdf80mmUrl", (object?)urlsPdfLocales.Ticket80 ?? DBNull.Value);
			await cmd.ExecuteNonQueryAsync();
		}

		internal static async Task ActualizarVoucherPostFalloComunicacionAsync(SqlConnection con, Guid voucherId)
		{
			const string sql = """
				UPDATE dbo.Voucher
				SET
					SunatStatus = CASE
						WHEN SunatStatus IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO', 'RECHAZADO', 'EXCEPCION') THEN SunatStatus
						ELSE 'EXCEPCION'
					END,
					UpdatedAt = SYSUTCDATETIME()
				WHERE Id = @Id;
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", voucherId);
			await cmd.ExecuteNonQueryAsync();
		}

		internal static async Task ActualizarVoucherPostConsultaAsync(SqlConnection con, Guid voucherId, FacturacionEnvioResultado consulta)
		{
			const string sql = """
				UPDATE dbo.Voucher
				SET
					SunatStatus = CASE
						WHEN @SunatStatus IN ('NO_ENVIADO', 'PENDIENTE', 'ACEPTADO', 'RECHAZADO', 'EXCEPCION') THEN @SunatStatus
						ELSE 'EXCEPCION'
					END,
					SunatDocumentId = COALESCE(@SunatDocumentId, SunatDocumentId),
					XmlUrl = COALESCE(@XmlUrl, XmlUrl),
					CdrUrl = COALESCE(@CdrUrl, CdrUrl),
					UpdatedAt = SYSUTCDATETIME()
				WHERE Id = @Id;
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", voucherId);
			cmd.Parameters.Add("@SunatStatus", SqlDbType.VarChar, 30).Value = NormalizarSunatStatusParaVoucher(consulta);
			cmd.Parameters.AddWithValue("@SunatDocumentId", (object?)consulta.DocumentId ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@XmlUrl", (object?)consulta.XmlUrl ?? DBNull.Value);
			cmd.Parameters.AddWithValue("@CdrUrl", (object?)consulta.CdrUrl ?? DBNull.Value);
			await cmd.ExecuteNonQueryAsync();
		}

		internal static async Task ActualizarVoucherPostAnulacionAsync(SqlConnection con, Guid voucherId, FacturacionEnvioResultado resultado)
		{
			const string sql = """
				UPDATE dbo.Voucher
				SET
					SunatStatus = 'ANULADO',
					UpdatedAt = SYSUTCDATETIME()
				WHERE Id = @Id;
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@Id", voucherId);
			await cmd.ExecuteNonQueryAsync();
		}

		internal static async Task EliminarVoucherAsync(SqlConnection con, Guid voucherId)
		{
			const string sql = """
				DELETE nf
				FROM dbo.SunatTransmissionFault nf
				INNER JOIN dbo.SunatTransmission st ON st.Id = nf.TransmissionId
				WHERE st.VoucherId = @VoucherId;

				DELETE nn
				FROM dbo.SunatTransmissionNote nn
				INNER JOIN dbo.SunatTransmission st ON st.Id = nn.TransmissionId
				WHERE st.VoucherId = @VoucherId;

				DELETE FROM dbo.SunatTransmission
				WHERE VoucherId = @VoucherId;

				DELETE FROM dbo.VoucherInstallment
				WHERE VoucherId = @VoucherId;

				DELETE FROM dbo.VoucherObservation
				WHERE VoucherId = @VoucherId;

				DELETE FROM dbo.VoucherItem
				WHERE VoucherId = @VoucherId;

				DELETE FROM dbo.VoucherLocation
				WHERE VoucherId = @VoucherId;

				DELETE FROM dbo.VoucherParty
				WHERE VoucherId = @VoucherId;

				DELETE FROM dbo.VoucherAdjustment
				WHERE VoucherId = @VoucherId
				   OR ReferencedVoucherId = @VoucherId;

				DELETE FROM dbo.Voucher
				WHERE Id = @VoucherId;
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@VoucherId", voucherId);
			await cmd.ExecuteNonQueryAsync();
		}

		private static async Task<int> ObtenerSiguienteIntentoAsync(SqlConnection con, Guid voucherId, string operationType)
		{
			const string sql = """
				SELECT ISNULL(MAX(AttemptNumber), 0) + 1
				FROM dbo.SunatTransmission
				WHERE VoucherId = @VoucherId
				  AND OperationType = @OperationType;
				""";

			using var cmd = new SqlCommand(sql, con);
			cmd.Parameters.AddWithValue("@VoucherId", voucherId);
			cmd.Parameters.AddWithValue("@OperationType", operationType);
			return Convert.ToInt32(await cmd.ExecuteScalarAsync());
		}

		private static string? ObtenerResumenTransmision(FacturacionEnvioResultado resultado)
		{
			if (!string.IsNullOrWhiteSpace(resultado.MensajeSunat))
			{
				return resultado.MensajeSunat;
			}

			var mensajesFault = ObtenerMensajesRespuestaApi(resultado.RespuestaApi, "faults");
			if (mensajesFault.Count > 0)
			{
				return string.Join(" | ", mensajesFault.Select(x => x.Message));
			}

			var mensajesNote = ObtenerMensajesRespuestaApi(resultado.RespuestaApi, "notes");
			if (mensajesNote.Count > 0)
			{
				return string.Join(" | ", mensajesNote.Select(x => x.Message));
			}

			if (!string.IsNullOrWhiteSpace(resultado.Mensaje))
			{
				return resultado.Mensaje;
			}

			return string.IsNullOrWhiteSpace(resultado.DetalleError) ? null : resultado.DetalleError;
		}

		private static async Task InsertarMensajesTransmisionAsync(SqlConnection con, Guid transmissionId, object? respuestaApi, string propertyName, string tableName)
		{
			var mensajes = ObtenerMensajesRespuestaApi(respuestaApi, propertyName);
			if (mensajes.Count == 0)
			{
				return;
			}

			var sql = $"""
				INSERT INTO {tableName}
				(
					Id,
					TransmissionId,
					Code,
					Message
				)
				VALUES
				(
					@Id,
					@TransmissionId,
					@Code,
					@Message
				);
				""";

			foreach (var (code, message) in mensajes)
			{
				using var cmd = new SqlCommand(sql, con);
				cmd.Parameters.AddWithValue("@Id", Guid.NewGuid());
				cmd.Parameters.AddWithValue("@TransmissionId", transmissionId);
				cmd.Parameters.AddWithValue("@Code", string.IsNullOrWhiteSpace(code) ? DBNull.Value : code);
				cmd.Parameters.AddWithValue("@Message", message);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		private static List<(string? Code, string Message)> ObtenerMensajesRespuestaApi(object? respuestaApi, string propertyName)
		{
			if (respuestaApi is not JsonElement element || element.ValueKind != JsonValueKind.Object)
			{
				return new List<(string? Code, string Message)>();
			}

			if (!element.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
			{
				return new List<(string? Code, string Message)>();
			}

			var mensajes = new List<(string? Code, string Message)>();
			foreach (var item in values.EnumerateArray())
			{
				if (item.ValueKind == JsonValueKind.String)
				{
					var texto = item.GetString();
					if (!string.IsNullOrWhiteSpace(texto))
					{
						mensajes.Add((null, texto));
					}

					continue;
				}

				if (item.ValueKind != JsonValueKind.Object)
				{
					var textoPlano = item.ToString();
					if (!string.IsNullOrWhiteSpace(textoPlano))
					{
						mensajes.Add((null, textoPlano));
					}

					continue;
				}

				var code =
					item.TryGetProperty("code", out var codeValue) ? codeValue.ToString() :
					item.TryGetProperty("faultcode", out var faultCodeValue) ? faultCodeValue.ToString() :
					null;
				var message =
					item.TryGetProperty("message", out var messageValue) ? messageValue.ToString() :
					item.TryGetProperty("faultstring", out var faultStringValue) ? faultStringValue.ToString() :
					item.ToString();

				if (!string.IsNullOrWhiteSpace(message))
				{
					mensajes.Add((code, message));
				}
			}

			return mensajes;
		}
	}
}
