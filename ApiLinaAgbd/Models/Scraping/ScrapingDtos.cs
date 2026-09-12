namespace ApiLinaAgbd.Models.Scraping;

public class ScrapingMatchDto
{
    public long Id { get; set; }
    public int? InternalProductId { get; set; }
    public string? InternalProductName { get; set; }
    public string? InternalProductSku { get; set; }
    public decimal? InternalProductPrice { get; set; }
    public long ScrapedProductId { get; set; }
    public string Store { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Url { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public string Decision { get; set; } = string.Empty;
    public DateTime ScrapedAt { get; set; }
}

public class ScrapingDecisionDto
{
    public int? ProductoId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
}
