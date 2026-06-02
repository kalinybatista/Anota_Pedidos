// Services/IUsuarioService.cs
using Anota_Pedidos.Models;

public interface IUsuarioService
{
    Task<UsuarioModel> CriarOuObterUsuarioAsync(string nome, string telefone);
    Task<string?> ObterTelefoneDescriptografadoAsync(int usuarioId);
    Task<string?> ObterTelefoneDescriptografadoPorNumeroAsync(string telefoneCriptografado);
}