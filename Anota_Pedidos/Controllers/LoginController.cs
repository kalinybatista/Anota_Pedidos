using Anota_Pedidos.Data;
using Anota_Pedidos.Models;
using Anota_Pedidos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text.Json;

namespace Anota_Pedidos.Controllers
{
    [RequireHttps]
    public class LoginController : Controller
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHashService _hashService;
        private readonly ICryptoService _cryptoService;

        public LoginController(ApplicationDbContext context, IEmailService emailService, IHashService hashService, ICryptoService cryptoService, IMemoryCache memoryCache)
        {
            _context = context;
            _emailService = emailService;
            _hashService = hashService;
            _cryptoService = cryptoService;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Limpar sessão anterior
            HttpContext.Session.Clear();

            // Verificar se tem email salvo no cookie
            var savedEmail = Request.Cookies["AdminEmail"];
            if (!string.IsNullOrEmpty(savedEmail))
            {
                ViewBag.SavedEmail = savedEmail;
                ViewBag.RememberMe = true;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // 🔥 PROTEÇÃO CONTRA FORÇA BRUTA
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var cacheKey = $"login_attempts_{ip}";

                // Obter tentativas atuais
                var tentativas = _memoryCache.TryGetValue(cacheKey, out int count) ? count : 0;

                if (tentativas >= 5)
                {
                    var tempoRestante = _memoryCache.TryGetValue($"{cacheKey}_expiry", out DateTime expiry)
                        ? (int)(expiry - DateTime.Now).TotalSeconds : 300;

                    return Json(new
                    {
                        success = false,
                        message = $"❌ Muitas tentativas de login. Aguarde {Math.Ceiling((double)tempoRestante / 60)} minutos antes de tentar novamente.",
                        remainingSeconds = tempoRestante
                    });
                }

                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Senha))
                {
                    return Json(new { success = false, message = "Preencha todos os campos!" });
                }

                string emailNormalizado = request.Email.Trim().ToLower();
                string emailCriptografado = _cryptoService.Encrypt(emailNormalizado);

                // Buscar admin
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.EmailCriptografado == emailCriptografado);

                if (admin == null)
                {
                    // 🔥 Incrementar tentativas
                    _memoryCache.Set(cacheKey, tentativas + 1, TimeSpan.FromMinutes(5));
                    _memoryCache.Set($"{cacheKey}_expiry", DateTime.Now.AddMinutes(5), TimeSpan.FromMinutes(5));
                    return Json(new { success = false, message = "E-mail ou senha incorretos!" });
                }

                // VERIFICAR SENHA COM HASH
                bool senhaValida = _hashService.VerifyPassword(request.Senha, admin.Senha);

                if (!senhaValida)
                {
                    // 🔥 Incrementar tentativas
                    _memoryCache.Set(cacheKey, tentativas + 1, TimeSpan.FromMinutes(5));
                    _memoryCache.Set($"{cacheKey}_expiry", DateTime.Now.AddMinutes(5), TimeSpan.FromMinutes(5));
                    return Json(new { success = false, message = "E-mail ou senha incorretos!" });
                }

                // 🔥 LOGIN BEM-SUCEDIDO - RESETAR TENTATIVAS
                _memoryCache.Remove(cacheKey);
                _memoryCache.Remove($"{cacheKey}_expiry");

                // Buscar estabelecimentos do admin
                var estabelecimentos = await _context.Estabelecimentos
                    .Where(e => e.Id_Admin == admin.Id_Admin)
                    .ToListAsync();

                // Salvar dados do admin na sessão
                HttpContext.Session.SetString("AdminNome", admin.Nome);
                HttpContext.Session.SetString("AdminEmail", request.Email);
                HttpContext.Session.SetInt32("AdminId", admin.Id_Admin);

                // Configurar "Lembrar-me"
                if (request.LembrarMe)
                {
                    Response.Cookies.Append("AdminEmail", request.Email, new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax
                    });
                }
                else
                {
                    Response.Cookies.Delete("AdminEmail");
                }

                // Redirecionar baseado na quantidade de estabelecimentos
                if (estabelecimentos.Count == 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Login realizado! Agora configure seu estabelecimento.",
                        redirectTo = "/Estabelecimento/Estabelecimento"
                    });
                }
                else if (estabelecimentos.Count == 1)
                {
                    var estabelecimento = estabelecimentos[0];
                    HttpContext.Session.SetInt32("EstabelecimentoId", estabelecimento.Id_Estabelecimento);

                    var estabelecimentoDTO = new
                    {
                        estabelecimento.Id_Estabelecimento,
                        estabelecimento.Nome_Estabelecimento,
                        estabelecimento.WhatsApp,
                        estabelecimento.Img_Estabelecimento,
                        estabelecimento.MensagemHero
                    };

                    HttpContext.Session.SetString("Estabelecimento", JsonSerializer.Serialize(estabelecimentoDTO));

                    return Json(new
                    {
                        success = true,
                        message = "Login realizado com sucesso!",
                        redirectTo = "/Admin/Pedidos"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = true,
                        message = "Selecione um estabelecimento para continuar.",
                        redirectTo = "/Estabelecimento/Selecionar"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no login: {ex.Message}");
                return Json(new { success = false, message = $"Erro ao fazer login: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult Cadastrar()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cadastrar([FromBody] CadastroRequest request)
        {
            try
            {
                // ===== VALIDAÇÕES =====
                if (string.IsNullOrWhiteSpace(request.Nome))
                    return Json(new { success = false, message = "Digite seu nome!" });

                if (string.IsNullOrWhiteSpace(request.Email))
                    return Json(new { success = false, message = "Digite seu e-mail!" });

                if (!IsValidEmail(request.Email))
                    return Json(new { success = false, message = "E-mail inválido!" });

                if (string.IsNullOrWhiteSpace(request.Senha))
                    return Json(new { success = false, message = "Digite uma senha!" });

                if (request.Senha.Length < 8)
                    return Json(new { success = false, message = "A senha deve ter no mínimo 8 caracteres!" });

                if (request.Senha != request.ConfirmarSenha)
                    return Json(new { success = false, message = "As senhas não coincidem!" });

                // 🔥 NORMALIZAR E CRIPTOGRAFAR EMAIL
                string emailNormalizado = request.Email.Trim().ToLower();
                string emailCriptografado = _cryptoService.Encrypt(emailNormalizado);

                // 🔥 VERIFICAR SE ADMIN JÁ EXISTE (BUSCA PELO EMAIL CRIPTOGRAFADO)
                var adminExistente = await _context.Admins
                    .FirstOrDefaultAsync(a => a.EmailCriptografado == emailCriptografado);


                if (adminExistente != null)
                    return Json(new { success = false, message = "Este e-mail já está cadastrado!" });

                // ===== GERAR HASH DA SENHA =====
                string senhaHash;
                try
                {
                    senhaHash = _hashService.HashPassword(request.Senha);
                    Console.WriteLine($"Hash gerado: {senhaHash}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao gerar hash: {ex.Message}");
                    return Json(new { success = false, message = "Erro interno ao processar senha" });
                }

                // 🔥 CRIAR NOVO ADMIN COM EMAIL CRIPTOGRAFADO
                var novoAdmin = new AdminModel
                {
                    Nome = request.Nome.Trim(),
                    EmailCriptografado = emailCriptografado,
                    Senha = senhaHash,
                    Data_Cadastro = DateTime.Now
                };

                _context.Admins.Add(novoAdmin);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Admin cadastrado com sucesso: {emailNormalizado} (criptografado)");

                return Json(new { success = true, message = "Cadastro realizado com sucesso! Faça login." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no cadastro: {ex.Message}");
                return Json(new { success = false, message = $"Erro ao cadastrar: {ex.Message}" });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public IActionResult RecuperarSenha()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarSenha([FromBody] RecuperarSenhaRequest request)
        {
            try
            {
                // 🔥 Normalizar e criptografar o email recebido
                string emailNormalizado = request.Email.Trim().ToLower();
                string emailCriptografado = _cryptoService.Encrypt(emailNormalizado);

                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.EmailCriptografado == emailCriptografado);

                if (admin == null)
                {
                    return Json(new { success = false, message = "E-mail não encontrado!" });
                }

                // 🔥 DESCRIPTOGRAFAR EMAIL PARA ENVIAR E-MAIL
                var emailDestino = _cryptoService.Decrypt(admin.EmailCriptografado);

                // Gerar token único
                var token = GerarTokenUnico();
                var expiracao = DateTime.Now.AddHours(2);

                // Salvar token no banco
                var tokenRecuperacao = new TokenRecuperacaoModel
                {
                    Email = admin.EmailCriptografado,  // 🔥 Salvar criptografado
                    Token = token,
                    Expiracao = expiracao,
                    Usado = false,
                    DataCriacao = DateTime.Now,
                };

                _context.TokensRecuperacao.Add(tokenRecuperacao);
                await _context.SaveChangesAsync();

                // 🔥 CHAMAR O MÉTODO PASSANDO APENAS O TOKEN (NÃO O LINK)
                var emailEnviado = await _emailService.EnviarEmailRecuperacaoSenha(admin.EmailCriptografado, admin.Nome, token);

                if (emailEnviado)
                {
                    return Json(new { success = true, message = "Instruções de recuperação enviadas para seu e-mail!" });
                }
                else
                {
                    return Json(new { success = false, message = "Erro ao enviar e-mail. Tente novamente mais tarde." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> RedefinirSenha(string token, string email)
        {
            // Se não veio token ou email, redireciona para login
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["Erro"] = "Link inválido!";
                return RedirectToAction("Login");
            }

            // 🔥 Normalizar e criptografar o email da URL
            string emailNormalizado = email.Trim().ToLower();
            string emailCriptografado = _cryptoService.Encrypt(emailNormalizado);

            // Verificar token
            var tokenValido = await _context.TokensRecuperacao
                .FirstOrDefaultAsync(t => t.Token == token && t.Email == emailCriptografado && !t.Usado && t.Expiracao > DateTime.Now);

            if (tokenValido == null)
            {
                TempData["Erro"] = "Link inválido ou expirado! Solicite uma nova recuperação.";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;
            ViewBag.Email = email;  // Email legível para exibição
            return View();
        }


        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NovaSenha))
                {
                    return Json(new { success = false, message = "Dados incompletos!" });
                }

                // 🔥 Normalizar e criptografar o email
                string emailNormalizado = request.Email.Trim().ToLower();
                string emailCriptografado = _cryptoService.Encrypt(emailNormalizado);

                // Verificar token
                var tokenValido = await _context.TokensRecuperacao
                    .FirstOrDefaultAsync(t => t.Token == request.Token && t.Email == emailCriptografado && !t.Usado && t.Expiracao > DateTime.Now);

                if (tokenValido == null)
                {
                    return Json(new { success = false, message = "Link inválido ou expirado! Solicite uma nova recuperação." });
                }

                if (request.NovaSenha.Length < 6)
                {
                    return Json(new { success = false, message = "A senha deve ter no mínimo 6 caracteres!" });
                }

                // Atualizar senha
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.EmailCriptografado == emailCriptografado);

                if (admin == null)
                {
                    return Json(new { success = false, message = "Usuário não encontrado!" });
                }

                // ATUALIZAR SENHA COM HASH
                admin.Senha = _hashService.HashPassword(request.NovaSenha);

                // Marcar token como usado
                tokenValido.Usado = true;

                await _context.SaveChangesAsync();

                // Limpar sessão se houver
                HttpContext.Session.Clear();

                return Json(new { success = true, message = "Senha redefinida com sucesso! Faça login." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erro: {ex.Message}" });
            }
        }

        private string GerarTokenUnico()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }


        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("AdminEmail");
            return RedirectToAction("Login");
        }

    }

    // Request Models
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Senha { get; set; }
        public bool LembrarMe { get; set; }
    }

    public class CadastroRequest
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string ConfirmarSenha { get; set; }
    }

    public class RecuperarSenhaRequest
    {
        public string Email { get; set; }
    }

    public class RedefinirSenhaRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NovaSenha { get; set; }
    }
}