using Anota_Pedidos.Data;
using Anota_Pedidos.Models;
using Anota_Pedidos.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Anota_Pedidos.Controllers
{
    [RequireHttps]
    public class PerfilController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHashService _hashService;
        private readonly ICryptoService _cryptoService;  // 🔥 ADICIONAR

        public PerfilController(
            ApplicationDbContext context,
            IEmailService emailService,
            IHashService hashService,
            ICryptoService cryptoService)  // 🔥 ADICIONAR PARÂMETRO
        {
            _context = context;
            _emailService = emailService;
            _hashService = hashService;
            _cryptoService = cryptoService;  // 🔥 ADICIONAR
        }

        // 🔥 MÉTODO AUXILIAR PARA DESCRIPTOGRAFAR EMAIL
        private string DescriptografarEmail(string emailCriptografado)
        {
            if (string.IsNullOrEmpty(emailCriptografado))
                return "";

            try
            {
                return _cryptoService.Decrypt(emailCriptografado);
            }
            catch
            {
                return emailCriptografado;
            }
        }

        // 🔥 MÉTODO AUXILIAR PARA CRIPTOGRAFAR EMAIL
        private string CriptografarEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "";

            return _cryptoService.Encrypt(email.Trim().ToLower());
        }

        // GET: Perfil/Perfil
        [HttpGet]
        public IActionResult Perfil()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction("Login", "Login");
            }
            return View();
        }

        // GET: Perfil/ObterDados
        [HttpGet]
        public async Task<IActionResult> ObterDados()
        {
            try
            {
                var adminId = HttpContext.Session.GetInt32("AdminId");
                if (adminId == null)
                {
                    return Json(new { success = false, message = "Admin não autenticado" });
                }

                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Id_Admin == adminId);

                if (admin == null)
                {
                    return Json(new { success = false, message = "Admin não encontrado" });
                }

                // 🔥 DESCRIPTOGRAFAR EMAIL PARA EXIBIÇÃO
                var emailDescriptografado = DescriptografarEmail(admin.EmailCriptografado);

                return Json(new
                {
                    success = true,
                    nome = admin.Nome,
                    email = emailDescriptografado,  // 🔥 Email legível
                    senha = "********",
                    dataCadastro = admin.Data_Cadastro.ToString("dd/MM/yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Perfil/EnviarCodigo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarCodigo()
        {
            try
            {
                var adminId = HttpContext.Session.GetInt32("AdminId");
                if (adminId == null)
                {
                    return Json(new { success = false, message = "Admin não autenticado" });
                }

                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Id_Admin == adminId);

                if (admin == null)
                {
                    return Json(new { success = false, message = "Admin não encontrado" });
                }

                // 🔥 DESCRIPTOGRAFAR EMAIL PARA ENVIAR E-MAIL
                var emailDestino = DescriptografarEmail(admin.EmailCriptografado);

                // Gerar código de 6 dígitos
                var random = new Random();
                var codigo = random.Next(100000, 999999).ToString();

                // Calcular expiração (5 minutos)
                var expiracao = DateTime.Now.AddMinutes(5);

                Console.WriteLine($"📧 Código gerado para {emailDestino}: {codigo}");
                Console.WriteLine($"📧 Expira em: {expiracao:HH:mm:ss}");

                // Salvar na sessão
                HttpContext.Session.SetString("PerfilCodigo", codigo);
                HttpContext.Session.SetString("PerfilCodigoExpiracao", expiracao.ToString("O"));

                // Enviar e-mail
                var corpoEmail = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Código de Verificação</title>
</head>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #f97316;'>🔐 Código de Verificação</h2>
        <p>Olá <strong>{admin.Nome}</strong>,</p>
        <p>Seu código de verificação é:</p>
        <div style='background: #f0fdf4; padding: 20px; text-align: center; border-radius: 10px; margin: 20px 0;'>
            <span style='font-size: 32px; font-weight: bold; color: #f97316; letter-spacing: 5px;'>{codigo}</span>
        </div>
        <p>Este código é válido até <strong>{expiracao:HH:mm}</strong>.</p>
        <p>Se você não solicitou essa verificação, ignore este e-mail.</p>
        <hr>
        <p style='color: #666; font-size: 12px;'>Alfa Prime - Sistema de Gestão</p>
    </div>
</body>
</html>";

                var emailEnviado = await _emailService.EnviarEmail(emailDestino, "🔐 Código de Verificação - Alfa Prime", corpoEmail);

                if (emailEnviado)
                {
                    return Json(new { success = true, message = $"Código enviado para {emailDestino}. Válido até {expiracao:HH:mm}." });
                }
                else
                {
                    return Json(new { success = false, message = "Erro ao enviar e-mail. Verifique as configurações de e-mail." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao enviar código: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Perfil/VerificarCodigo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerificarCodigo([FromBody] CodigoRequest request)
        {
            try
            {
                Console.WriteLine($"🔍 Verificando código recebido: {request?.Codigo}");

                var codigoSalvo = HttpContext.Session.GetString("PerfilCodigo");
                var expiracaoStr = HttpContext.Session.GetString("PerfilCodigoExpiracao");

                Console.WriteLine($"📦 Código salvo: {codigoSalvo}");
                Console.WriteLine($"⏰ Expiração: {expiracaoStr}");
                Console.WriteLine($"⏰ Agora: {DateTime.Now:HH:mm:ss}");

                if (string.IsNullOrEmpty(codigoSalvo))
                {
                    return Json(new { success = false, message = "Nenhum código foi solicitado. Clique em 'Reenviar código'." });
                }

                // Verificar expiração
                if (!string.IsNullOrEmpty(expiracaoStr))
                {
                    if (DateTime.TryParse(expiracaoStr, out DateTime expiracao))
                    {
                        if (DateTime.Now > expiracao)
                        {
                            HttpContext.Session.Remove("PerfilCodigo");
                            HttpContext.Session.Remove("PerfilCodigoExpiracao");
                            return Json(new { success = false, message = "Código expirado! Solicite um novo." });
                        }
                    }
                }

                if (codigoSalvo != request?.Codigo)
                {
                    return Json(new { success = false, message = "Código incorreto! Tente novamente." });
                }

                HttpContext.Session.SetString("PerfilVerificado", "true");
                Console.WriteLine("✅ Código verificado com sucesso!");

                return Json(new { success = true, message = "Código verificado! Agora você pode editar seus dados." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro na verificação: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Perfil/Atualizar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar([FromBody] PerfilRequest request)
        {
            try
            {
                var perfilVerificado = HttpContext.Session.GetString("PerfilVerificado");
                if (perfilVerificado != "true")
                {
                    return Json(new { success = false, message = "Verifique o código de segurança primeiro!" });
                }

                var adminId = HttpContext.Session.GetInt32("AdminId");
                if (adminId == null)
                {
                    return Json(new { success = false, message = "Admin não autenticado" });
                }

                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Id_Admin == adminId);

                if (admin == null)
                {
                    return Json(new { success = false, message = "Admin não encontrado" });
                }

                // Validar
                if (string.IsNullOrWhiteSpace(request.Nome))
                {
                    return Json(new { success = false, message = "Nome é obrigatório" });
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return Json(new { success = false, message = "E-mail é obrigatório" });
                }

                if (!request.Email.Contains("@") || !request.Email.Contains("."))
                {
                    return Json(new { success = false, message = "E-mail inválido" });
                }

                // 🔥 CRIPTOGRAFAR O NOVO EMAIL
                var emailNormalizado = request.Email.Trim().ToLower();
                var emailCriptografado = CriptografarEmail(emailNormalizado);

                // Verificar e-mail duplicado
                var adminExistente = await _context.Admins
                    .FirstOrDefaultAsync(a => a.EmailCriptografado == emailCriptografado && a.Id_Admin != adminId);

                if (adminExistente != null)
                {
                    return Json(new { success = false, message = "Este e-mail já está em uso" });
                }

                // Atualizar
                admin.Nome = request.Nome;
                admin.EmailCriptografado = emailCriptografado;  // 🔥 Salvar criptografado

                if (!string.IsNullOrEmpty(request.Senha))
                {
                    if (request.Senha.Length < 6)
                    {
                        return Json(new { success = false, message = "A senha deve ter no mínimo 6 caracteres" });
                    }
                    admin.Senha = _hashService.HashPassword(request.Senha);
                }

                await _context.SaveChangesAsync();

                // Atualizar sessão
                HttpContext.Session.SetString("AdminNome", admin.Nome);
                HttpContext.Session.SetString("AdminEmail", request.Email);  // Email legível para sessão

                // Limpar verificação
                HttpContext.Session.Remove("PerfilVerificado");
                HttpContext.Session.Remove("PerfilCodigo");
                HttpContext.Session.Remove("PerfilCodigoExpiracao");

                return Json(new { success = true, message = "Perfil atualizado com sucesso!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Perfil/DebugCodigo
        [HttpGet]
        public IActionResult DebugCodigo()
        {
            var codigo = HttpContext.Session.GetString("PerfilCodigo");
            var expiracao = HttpContext.Session.GetString("PerfilCodigoExpiracao");
            return Json(new { codigo = codigo, expiracao = expiracao });
        }
    }

    // Request Models
    public class CodigoRequest
    {
        public string Codigo { get; set; }
    }

    public class PerfilRequest
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
    }
}