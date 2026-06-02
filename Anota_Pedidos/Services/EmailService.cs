using iText.Layout.Element;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading.Tasks;

namespace Anota_Pedidos.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarEmailRecuperacaoSenha(string email, string nome, string token);
        Task<bool> EnviarEmailBoasVindas(string email, string nome);
        Task<bool> EnviarEmailNotificacao(string email, string assunto, string mensagem);
        Task<bool> EnviarEmail(string email, string assunto, string corpoHtml);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly ICryptoService _cryptoService;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            ICryptoService cryptoService)
        {
            _configuration = configuration;
            _logger = logger;
            _cryptoService = cryptoService;
        }

        public async Task<bool> EnviarEmailRecuperacaoSenha(string email, string nome, string token)
        {
            try
            {
                // 🔥 DESCRIPTOGRAFAR O EMAIL
                string emailLegivel;
                try
                {
                    emailLegivel = _cryptoService.Decrypt(email);
                }
                catch
                {
                    emailLegivel = email;
                }

                // 🔥 BASE URL DO SITE (NÃO USAR O TOKEN COMO URL)
                var baseUrl = _configuration["App:BaseUrl"];
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = "https://kaliny27-001-site1.ktempurl.com";
                }
                baseUrl = baseUrl.TrimEnd('/');

                // 🔥 CONSTRUIR O LINK USANDO APENAS O TOKEN (NÃO A URL COMPLETA)
                var linkRecuperacao = $"{baseUrl}/Login/RedefinirSenha?token={token}&email={Uri.EscapeDataString(emailLegivel)}";

                Console.WriteLine($"📧 Email legível: {emailLegivel}");
                Console.WriteLine($"🔗 Token: {token}");
                Console.WriteLine($"🔗 Link completo: {linkRecuperacao}");

                var corpoEmail = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Redefinição de Senha</title>
</head>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: white; border-radius: 16px; padding: 30px; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>

            <h1 style='color: #0f172a; font-size: 24px;'>Recuperação de Senha</h1>
            <p>Olá <strong>{nome}</strong>,</p>
            <p>Recebemos uma solicitação para redefinir sua senha.</p>
            
            <div style='background: #f0fdf4; padding: 10px; border-radius: 8px; text-align: center; margin: 15px 0;'>
                📧 <strong>Conta:</strong> {emailLegivel}
            </div>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{linkRecuperacao}' style='background: linear-gradient(135deg, #f97316, #ea580c); color: white; padding: 12px 30px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>
                    🔐 Redefinir minha senha
                </a>
            </div>
            
            <div style='background: #fef2f2; padding: 15px; border-radius: 10px; margin: 20px 0; font-size: 14px; color: #991b1b;'>
                ⚠️ <strong>Atenção:</strong> Este link é válido por <strong>2 horas</strong>.<br>
                Se você não solicitou essa alteração, ignore este e-mail.
            </div>
            
            <p>Se o botão não funcionar, copie e cole o link abaixo no seu navegador:</p>
            <p style='word-break: break-all; font-size: 12px; color: #64748b; background: #f8fafc; padding: 10px; border-radius: 8px;'>
                {linkRecuperacao}
            </p>
            
            <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;'>
            <p style='font-size: 12px; color: #94a3b8; text-align: center;'>
                🔒 Este é um e-mail automático, por favor não responda.<br>
                © {DateTime.Now.Year} KBD Systems - Todos os direitos reservados
            </p>
        </div>
    </div>
</body>
</html>";

                return await EnviarEmail(emailLegivel, "KBD Systems - Recuperação de Senha", corpoEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar e-mail de recuperação para {email}");
                return false;
            }
        }

        public async Task<bool> EnviarEmailBoasVindas(string email, string nome)
        {
            // 🔥 Descriptografar email se necessário
            string emailDestino;
            try
            {
                emailDestino = _cryptoService.Decrypt(email);
            }
            catch
            {
                emailDestino = email;
            }

            var corpoEmail = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .card {{ background: white; border-radius: 16px; padding: 30px; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .logo {{ text-align: center; margin-bottom: 20px; }}
        .logo h2 {{ color: #f97316; margin: 0; }}
        h1 {{ color: #0f172a; font-size: 24px; margin-bottom: 20px; }}
        p {{ color: #475569; line-height: 1.6; margin-bottom: 20px; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e2e8f0; color: #94a3b8; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='card'>
            <h1>Bem-vindo ao Alfa Prime!</h1>
            <p>Olá <strong>{nome}</strong>,</p>
            <p>Seu cadastro foi realizado com sucesso! Agora você pode gerenciar seu estabelecimento, receber pedidos e muito mais.</p>
            <p>Para começar, faça login no sistema e configure seu estabelecimento.</p>
            <div class='footer'>
                <p>© {DateTime.Now.Year} KBD Systems - Todos os direitos reservados</p>
            </div>
        </div>
    </div>
</body>
</html>";

            return await EnviarEmail(emailDestino, "🎉 KBD Systems - Bem-vindo!", corpoEmail);
        }

        public async Task<bool> EnviarEmailNotificacao(string email, string assunto, string mensagem)
        {
            // 🔥 Descriptografar email se necessário
            string emailDestino;
            try
            {
                emailDestino = _cryptoService.Decrypt(email);
            }
            catch
            {
                emailDestino = email;
            }

            var corpoEmail = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .card {{ background: white; border-radius: 16px; padding: 30px; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .logo {{ text-align: center; margin-bottom: 20px; }}
        .logo h2 {{ color: #f97316; margin: 0; }}
        p {{ color: #475569; line-height: 1.6; margin-bottom: 20px; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e2e8f0; color: #94a3b8; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='card'>
            <p>{mensagem}</p>
            <div class='footer'>
                <p>© {DateTime.Now.Year}KBD Systems - Todos os direitos reservados</p>
            </div>
        </div>
    </div>
</body>
</html>";

            return await EnviarEmail(emailDestino, assunto, corpoEmail);
        }

        public async Task<bool> EnviarEmail(string email, string assunto, string corpoHtml)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("KBD Systems", smtpSettings["FromEmail"]));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = assunto;
                message.Body = new TextPart(TextFormat.Html)
                {
                    Text = corpoHtml
                };

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    smtpSettings["SmtpServer"],
                    int.Parse(smtpSettings["Port"]),
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"E-mail enviado com sucesso para {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar e-mail para {email}");
                return false;
            }
        }
    }
}


            //< div class= 'logo' >
            //    < h2 >🔥 Alfa Prime</h2>
            //</div>