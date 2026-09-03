using System;

namespace ApiLinaAgbd.Models.Facturacion.SunatTransmission
{
	public class SunatTransmissionDto
	{
		public Guid Id { get; set; }
		public Guid VoucherId { get; set; }
		public int AttemptNumber { get; set; }
		public string OperationType { get; set; } = string.Empty;
		public string TransmissionStatus { get; set; } = string.Empty;
		public int? HttpStatus { get; set; }
		public string? SunatStatus { get; set; }
		public string? SunatDocumentId { get; set; }
		public string? ErrorMessage { get; set; }
		public DateTime? RespondedAt { get; set; }
		public DateTime CreatedAt { get; set; }
		public int? ResponseTimeMs { get; set; }

		// Información del Voucher asociado
		public string? VoucherTypeCode { get; set; }
		public string? Series { get; set; }
		public string? Number { get; set; }
		public decimal? Total { get; set; }
		public string? CustomerName { get; set; }
	}
}
