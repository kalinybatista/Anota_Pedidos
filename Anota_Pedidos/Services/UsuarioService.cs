// Services/UsuarioService.cs
using Anota_Pedidos.Data;
using Anota_Pedidos.Models;
using Microsoft.EntityFrameworkCore;

namespace Anota_Pedidos.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICryptoService _crypto;

        public UsuarioService(ApplicationDbContext context, ICryptoService crypto)
        {
            _context = context;
            _crypto = crypto;
        }

        public async Task<UsuarioModel> CriarOuObterUsuarioAsync(string nome, string telefone)
        {
            // Normalizar telefone
            string telefoneNormalizado = new string(telefone.Where(char.IsDigit).ToArray());

            // Criptografar o telefone
            string telefoneCriptografado = _crypto.Encrypt(telefoneNormalizado);

            // Buscar por telefone criptografado
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Telefone == telefoneCriptografado);

            if (usuario == null)
            {
                usuario = new UsuarioModel
                {
                    Nome = nome,
                    Telefone = telefoneCriptografado
                };
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
            }

            return usuario;
        }

        public async Task<string?> ObterTelefoneDescriptografadoAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id_Usuario == usuarioId);

            if (usuario == null || string.IsNullOrEmpty(usuario.Telefone))
                return null;

            return _crypto.Decrypt(usuario.Telefone);
        }

        public async Task<string?> ObterTelefoneDescriptografadoPorNumeroAsync(string telefoneCriptografado)
        {
            if (string.IsNullOrEmpty(telefoneCriptografado))
                return null;

            return _crypto.Decrypt(telefoneCriptografado);
        }
    }
}