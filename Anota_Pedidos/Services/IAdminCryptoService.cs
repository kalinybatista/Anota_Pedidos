// Services/IAdminCryptoService.cs
using Anota_Pedidos.Data;
using Anota_Pedidos.Models;
using Anota_Pedidos.Services;
using Microsoft.EntityFrameworkCore;

public interface IAdminCryptoService
{
    Task<AdminModel?> ObterAdminPorEmailAsync(string email);
    Task<AdminModel?> ObterAdminPorIdAsync(int id);
    Task<bool> VerificarCredenciaisAsync(string email, string senha);
}

// Services/AdminCryptoService.cs
public class AdminCryptoService : IAdminCryptoService
{
    private readonly ApplicationDbContext _context;
    private readonly ICryptoService _crypto;
    private readonly IHashService _hash;

    public AdminCryptoService(ApplicationDbContext context, ICryptoService crypto, IHashService hash)
    {
        _context = context;
        _crypto = crypto;
        _hash = hash;
    }

    public async Task<AdminModel?> ObterAdminPorEmailAsync(string email)
    {
        // Normalizar email
        string emailNormalizado = email.Trim().ToLower();

        // Criptografar email para busca
        string emailCriptografado = _crypto.Encrypt(emailNormalizado);

        return await _context.Admins
            .FirstOrDefaultAsync(a => a.EmailCriptografado == emailCriptografado);
    }

    public async Task<AdminModel?> ObterAdminPorIdAsync(int id)
    {
        return await _context.Admins
            .FirstOrDefaultAsync(a => a.Id_Admin == id);
    }

    public async Task<bool> VerificarCredenciaisAsync(string email, string senha)
    {
        var admin = await ObterAdminPorEmailAsync(email);

        if (admin == null)
            return false;

        return _hash.VerifyPassword(senha, admin.Senha);
    }
}