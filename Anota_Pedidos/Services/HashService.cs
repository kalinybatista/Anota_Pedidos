using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Anota_Pedidos.Services
{
    public interface IHashService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class HashService : IHashService
    {
        // Parâmetros padrão compatíveis com ASP.NET Core Identity
        private const int Iterations = 10000;
        private const int SaltSize = 128 / 8; // 16 bytes
        private const int HashSize = 256 / 8; // 32 bytes

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Senha não pode ser vazia");

            // Gerar salt aleatório
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // Gerar hash usando PBKDF2 (compatível com todas as versões do .NET)
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: HashSize);

            // Formato: {iterations}.{salt}.{hash}
            // Usamos iterations como versão para compatibilidade
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string hashArmazenado)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashArmazenado))
                return false;

            try
            {
                // Verificar formato: iterations.salt.hash
                var partes = hashArmazenado.Split('.');
                if (partes.Length != 3)
                {
                    // Tentar compatibilidade com BCrypt (formato antigo)
                    return VerifyPasswordBCrypt(password, hashArmazenado);
                }

                // Extrair iterations, salt e hash
                if (!int.TryParse(partes[0], out int iterations))
                    return false;

                byte[] salt = Convert.FromBase64String(partes[1]);
                byte[] hashEsperado = Convert.FromBase64String(partes[2]);

                // Gerar hash da senha fornecida com o mesmo salt
                byte[] hashGerado = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: iterations,
                    numBytesRequested: HashSize);

                // Comparar os hashes
                return CryptographicOperations.FixedTimeEquals(hashGerado, hashEsperado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar hash: {ex.Message}");
                // Fallback: tentar BCrypt
                return VerifyPasswordBCrypt(password, hashArmazenado);
            }
        }

        // Método de fallback para compatibilidade com senhas antigas (BCrypt)
        private bool VerifyPasswordBCrypt(string password, string hash)
        {
            try
            {
                // Tentar usar BCrypt se disponível
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}