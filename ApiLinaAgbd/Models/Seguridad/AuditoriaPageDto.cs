namespace ApiLinaAgbd.Models.Seguridad;

public sealed class AuditoriaPageDto
{
	public IReadOnlyList<AuditoriaDto> Items { get; init; } = [];
	public int Page { get; init; }
	public int PageSize { get; init; }
	public int TotalItems { get; init; }
	public int TotalPages { get; init; }
}
