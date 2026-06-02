using Anota_Pedidos.Data;
using Anota_Pedidos.Filters;
using Anota_Pedidos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Anota_Pedidos.Controllers
{
    [RequireHttps]
    [ServiceFilter(typeof(AuthFilter))]
    public class EstabelecimentoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EstabelecimentoController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Estabelecimento()
        {
            return View();
        }

        // GET: Estabelecimento/Configurar

        public async Task<IActionResult> Configurar()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login", "Login");
            }

            // Buscar estabelecimento do admin atual
            var estabelecimento = await _context.Estabelecimentos
                .FirstOrDefaultAsync(e => e.Id_Admin == adminId);

            // Se não existir, criar um novo
            if (estabelecimento == null)
            {
                estabelecimento = new EstabelecimentoModel
                {
                    Id_Admin = adminId.Value,
                    Nome_Estabelecimento = "",
                    WhatsApp = "",
                    MensagemHero = "Espetinhos e jantinhas feitos com amor ❤️",
                    Data_Atualizacao = DateTime.Now
                };
                ViewBag.Novo = true;
            }
            else
            {
                ViewBag.Novo = false;

                // Salvar na sessão
                var estabelecimentoDTO = new EstabelecimentoSessionDTO
                {
                    Id_Estabelecimento = estabelecimento.Id_Estabelecimento,
                    Nome_Estabelecimento = estabelecimento.Nome_Estabelecimento,
                    WhatsApp = estabelecimento.WhatsApp,
                    Img_Estabelecimento = estabelecimento.Img_Estabelecimento,
                    MensagemHero = estabelecimento.MensagemHero ?? "Espetinhos e jantinhas feitos com amor ❤️" // ADICIONAR
                };

                HttpContext.Session.SetString("Estabelecimento", JsonSerializer.Serialize(estabelecimentoDTO));
                HttpContext.Session.SetInt32("EstabelecimentoId", estabelecimento.Id_Estabelecimento);
            }

            return View(estabelecimento);
        }

        // POST: Estabelecimento/Salvar (Criar ou Atualizar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar([FromBody] EstabelecimentoRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Dados inválidos" });

                if (string.IsNullOrWhiteSpace(request.NomeEstabelecimento))
                    return Json(new { success = false, message = "Nome do estabelecimento é obrigatório" });

                if (string.IsNullOrWhiteSpace(request.WhatsApp))
                    return Json(new { success = false, message = "WhatsApp é obrigatório" });

                var adminId = HttpContext.Session.GetInt32("AdminId");
                if (adminId == null)
                {
                    return Json(new { success = false, message = "Admin não autenticado" });
                }

                var estabelecimento = await _context.Estabelecimentos
                    .FirstOrDefaultAsync(e => e.Id_Admin == adminId);

                bool isNew = false;

                if (estabelecimento == null)
                {
                    estabelecimento = new EstabelecimentoModel();
                    _context.Estabelecimentos.Add(estabelecimento);
                    isNew = true;
                }

                estabelecimento.Id_Admin = adminId.Value;
                estabelecimento.Nome_Estabelecimento = request.NomeEstabelecimento;
                estabelecimento.MensagemHero = request.MensagemHero ?? "Espetinhos e jantinhas feitos com amor ❤️";
                estabelecimento.WhatsApp = request.WhatsApp;
                estabelecimento.Data_Atualizacao = DateTime.Now;

                // Salvar imagem se fornecida
                if (!string.IsNullOrEmpty(request.ImagemBase64) && request.ImagemBase64.Contains("base64"))
                {
                    var imagemPath = await SalvarImagemBase64(request.ImagemBase64);
                    if (!string.IsNullOrEmpty(imagemPath))
                    {
                        // Remover imagem antiga se existir
                        if (!string.IsNullOrEmpty(estabelecimento.Img_Estabelecimento))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                                estabelecimento.Img_Estabelecimento.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                                System.IO.File.Delete(oldImagePath);
                        }
                        estabelecimento.Img_Estabelecimento = imagemPath;
                    }
                }

                await _context.SaveChangesAsync();

                // Salvar na sessão
                var estabelecimentoDTO = new EstabelecimentoSessionDTO
                {
                    Id_Estabelecimento = estabelecimento.Id_Estabelecimento,
                    Nome_Estabelecimento = estabelecimento.Nome_Estabelecimento,
                    WhatsApp = estabelecimento.WhatsApp,
                    Img_Estabelecimento = estabelecimento.Img_Estabelecimento
                };

                HttpContext.Session.SetString("Estabelecimento", JsonSerializer.Serialize(estabelecimentoDTO));
                HttpContext.Session.SetInt32("EstabelecimentoId", estabelecimento.Id_Estabelecimento);

                string mensagem = isNew ? "Estabelecimento criado com sucesso!" : "Configurações salvas com sucesso!";

                return Json(new
                {
                    success = true,
                    message = mensagem,
                    isNew = isNew,
                    nome = estabelecimento.Nome_Estabelecimento,
                    imagem = estabelecimento.Img_Estabelecimento ?? "",
                    mensagemHero = estabelecimento.MensagemHero ?? "Espetinhos e jantinhas feitos com amor ❤️" // ADICIONAR
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Estabelecimento/Obter
        [HttpGet]
        public async Task<IActionResult> Obter()
        {
            try
            {
                var adminId = HttpContext.Session.GetInt32("AdminId");

                if (adminId == null)
                {
                    return Json(new { success = false, existe = false, message = "Admin não autenticado" });
                }

                var estabelecimento = await _context.Estabelecimentos
                    .FirstOrDefaultAsync(e => e.Id_Admin == adminId);

                if (estabelecimento != null)
                {
                    var estabelecimentoDTO = new EstabelecimentoSessionDTO
                    {
                        Id_Estabelecimento = estabelecimento.Id_Estabelecimento,
                        Nome_Estabelecimento = estabelecimento.Nome_Estabelecimento,
                        WhatsApp = estabelecimento.WhatsApp,
                        Img_Estabelecimento = estabelecimento.Img_Estabelecimento,
                        MensagemHero = estabelecimento.MensagemHero ?? "Espetinhos e jantinhas feitos com amor ❤️" // ADICIONAR
                    };

                    HttpContext.Session.SetString("Estabelecimento", JsonSerializer.Serialize(estabelecimentoDTO));
                    HttpContext.Session.SetInt32("EstabelecimentoId", estabelecimento.Id_Estabelecimento);
                }

                return Json(new
                {
                    success = true,
                    existe = estabelecimento != null,
                    nome = estabelecimento?.Nome_Estabelecimento ?? "",
                    whatsapp = estabelecimento?.WhatsApp ?? "",
                    imagem = estabelecimento?.Img_Estabelecimento ?? "",
                    mensagemHero = estabelecimento?.MensagemHero ?? "Espetinhos e jantinhas feitos com amor ❤️" // ADICIONAR
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Estabelecimento/ObterDaSessao
        [HttpGet]
        public IActionResult ObterDaSessao()
        {
            try
            {
                var estabelecimentoJson = HttpContext.Session.GetString("Estabelecimento");

                if (string.IsNullOrEmpty(estabelecimentoJson))
                    return Json(new { success = false, existe = false });

                var estabelecimento = JsonSerializer.Deserialize<EstabelecimentoSessionDTO>(estabelecimentoJson);

                var imagem = estabelecimento?.Img_Estabelecimento ?? "";
                if (!string.IsNullOrEmpty(imagem) && !imagem.StartsWith("/") && !imagem.StartsWith("http"))
                {
                    imagem = "/" + imagem;
                }

                return Json(new
                {
                    success = true,
                    existe = true,
                    nome = estabelecimento?.Nome_Estabelecimento ?? "",
                    whatsapp = estabelecimento?.WhatsApp ?? "",
                    imagem = imagem,
                    mensagemHero = estabelecimento?.MensagemHero ?? "Espetinhos e jantinhas feitos com amor ❤️" // ADICIONAR
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Estabelecimento/Selecionar (para admin com múltiplos estabelecimentos)
        [HttpGet]
        public async Task<IActionResult> Selecionar()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");

            if (adminId == null)
            {
                return RedirectToAction("Login", "Login");
            }

            var estabelecimentos = await _context.Estabelecimentos
                .Where(e => e.Id_Admin == adminId)
                .ToListAsync();

            // LOG para debug
            Console.WriteLine($"Admin ID: {adminId}");
            Console.WriteLine($"Quantidade de estabelecimentos: {estabelecimentos.Count}");

            // Se não tem nenhum, redireciona para criar (USAR A ROTA CORRETA)
            if (estabelecimentos.Count == 0)
            {
                Console.WriteLine("Redirecionando para Admin/Estabelecimento (nenhum estabelecimento)");
                return RedirectToAction("Estabelecimento", "Admin");  // <- MUDAR PARA Admin/Estabelecimento
            }

            // Se só tem um estabelecimento, redireciona direto
            if (estabelecimentos.Count == 1)
            {
                Console.WriteLine($"Redirecionando para Admin/Pedidos (ID: {estabelecimentos[0].Id_Estabelecimento})");
                HttpContext.Session.SetInt32("EstabelecimentoId", estabelecimentos[0].Id_Estabelecimento);
                return RedirectToAction("Pedidos", "Admin");
            }

            // Múltiplos estabelecimentos - mostrar tela de seleção
            Console.WriteLine($"Mostrando tela de seleção com {estabelecimentos.Count} estabelecimentos");
            return View(estabelecimentos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Selecionar(int id)
        {
            HttpContext.Session.SetInt32("EstabelecimentoId", id);
            return RedirectToAction("Pedidos", "Admin");
        }

        private async Task<string?> SalvarImagemBase64(string base64Image)
        {
            try
            {
                var base64Data = base64Image;
                if (base64Image.Contains(","))
                {
                    base64Data = base64Image.Substring(base64Image.IndexOf(",") + 1);
                }

                var bytes = Convert.FromBase64String(base64Data);

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "estabelecimentos");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string extensao = ".jpg";
                string nomeUnico = $"estabelecimento_{DateTime.Now.Ticks}{extensao}";
                string caminhoArquivo = Path.Combine(uploadsFolder, nomeUnico);

                await System.IO.File.WriteAllBytesAsync(caminhoArquivo, bytes);

                return $"/uploads/estabelecimentos/{nomeUnico}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar imagem: {ex.Message}");
                return null;
            }
        }
    }

    // DTO para sessão
    public class EstabelecimentoSessionDTO
    {
        public int Id_Estabelecimento { get; set; }
        public string Nome_Estabelecimento { get; set; }
        public string WhatsApp { get; set; }
        public string Img_Estabelecimento { get; set; }
        public string MensagemHero { get; set; }
    }

    // Classe para a requisição
    public class EstabelecimentoRequest
    {
        public string NomeEstabelecimento { get; set; }
        public string WhatsApp { get; set; }
        public string ImagemBase64 { get; set; }
        public string MensagemHero { get; set; }
    }
}