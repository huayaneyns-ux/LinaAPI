using ApiLinaAgbd.Models.Integracion;

namespace ApiLinaAgbd.Services.Integracion;

public interface IIntegracionClientesService
{
	List<ClienteIntegracionDto> ListarClientes();
	(ClienteIntegracionDto Cliente, bool YaExistia, string? CampoDuplicado) RegistrarClienteExterno(ClienteWebhookDto cliente);
	Task NotificarClienteNuevoAsync(ClienteIntegracionDto cliente);
}
