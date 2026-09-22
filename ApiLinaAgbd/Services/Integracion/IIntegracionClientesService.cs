using ApiLinaAgbd.Models.Integracion;

namespace ApiLinaAgbd.Services.Integracion;

public interface IIntegracionClientesService
{
	List<ClienteIntegracionDto> ListarClientes();
	(ClienteIntegracionDto Cliente, bool YaExistia) RegistrarClienteExterno(ClienteWebhookDto cliente);
	Task NotificarClienteNuevoAsync(ClienteIntegracionDto cliente);
}
