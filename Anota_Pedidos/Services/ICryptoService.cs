// Services/ICryptoService.cs
namespace Anota_Pedidos.Services
{
    public interface ICryptoService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        bool TryDecrypt(string cipherText, out string plainText);
    }
}