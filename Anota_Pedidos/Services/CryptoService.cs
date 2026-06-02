// Services/CryptoService.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Anota_Pedidos.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public CryptoService(IConfiguration configuration)
        {
            // Chave de 32 bytes (256 bits) e IV de 16 bytes (128 bits)
            var keyBase64 = configuration["Encryption:Key"];
            var ivBase64 = configuration["Encryption:IV"];

            if (string.IsNullOrEmpty(keyBase64) || string.IsNullOrEmpty(ivBase64))
            {
                // Gerar chave e IV automaticamente se não existirem
                using var aes = Aes.Create();
                aes.GenerateKey();
                aes.GenerateIV();
                _key = aes.Key;
                _iv = aes.IV;

                // Salvar no appsettings.json (apenas para desenvolvimento)
                Console.WriteLine($"🔑 Chave AES: {Convert.ToBase64String(_key)}");
                Console.WriteLine($"🔑 IV AES: {Convert.ToBase64String(_iv)}");
            }
            else
            {
                _key = Convert.FromBase64String(keyBase64);
                _iv = Convert.FromBase64String(ivBase64);
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);
            sw.Write(plainText);
            sw.Flush();
            cs.FlushFinalBlock();

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                var cipherBytes = Convert.FromBase64String(cipherText);

                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch
            {
                // Se falhar, pode ser texto não criptografado (dados antigos)
                return cipherText;
            }
        }

        public bool TryDecrypt(string cipherText, out string plainText)
        {
            try
            {
                plainText = Decrypt(cipherText);
                return true;
            }
            catch
            {
                plainText = cipherText;
                return false;
            }
        }
    }
}