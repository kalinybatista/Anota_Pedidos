using Anota_Pedidos.Data;
using Anota_Pedidos.Hubs;
using Anota_Pedidos.Models;
using Anota_Pedidos.Services;
using Anota_Pedidos.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Anota_Pedidos.Controllers
{
    [RequireHttps]
    public class UsuarioController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<PedidoHub> _pedidoHub;
        private readonly ICryptoService _cryptoService;

        public UsuarioController(
            IAdminService adminService,
            ApplicationDbContext context,
            IHubContext<PedidoHub> pedidoHub,
            ICryptoService cryptoService)
        {
            _adminService = adminService;
            _context = context;
            _pedidoHub = pedidoHub;
            _cryptoService = cryptoService;
        }

        public async Task<IActionResult> Cardapio()
        {
            try
            {
                var estabelecimento = await _context.Estabelecimentos.FirstOrDefaultAsync();

                if (estabelecimento != null)
                {
                    var estabelecimentoDTO = new EstabelecimentoSessionDTO
                    {
                        Id_Estabelecimento = estabelecimento.Id_Estabelecimento,
                        Nome_Estabelecimento = estabelecimento.Nome_Estabelecimento,
                        WhatsApp = estabelecimento.WhatsApp ?? "",
                        Img_Estabelecimento = estabelecimento.Img_Estabelecimento ?? "",
                        MensagemHero = estabelecimento.MensagemHero ?? ""
                    };

                    HttpContext.Session.SetString("Estabelecimento", JsonSerializer.Serialize(estabelecimentoDTO));
                    HttpContext.Session.SetInt32("EstabelecimentoId", estabelecimento.Id_Estabelecimento);
                }

                var viewModel = await _adminService.ObterGerenciarCardapioAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Erro = ex.Message;
                return View(new GerenciarCardapioViewModel());
            }
        }



        private DateTime GetBrasiliaTime()
        {
            return DateTime.UtcNow.AddHours(-3); // Horário de Brasília
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarPedido([FromBody] PedidoRequest request)
        {
            try
            {
                var estabelecimento = await _context.Estabelecimentos.FirstOrDefaultAsync();
                if (estabelecimento == null)
                    return Json(new { success = false, message = "Nenhum estabelecimento cadastrado" });

                // NORMALIZAR TELEFONE
                string telefoneNormalizado = new string(request.TelefoneCliente.Where(char.IsDigit).ToArray());

                // 🔥 CRIPTOGRAFAR O TELEFONE PARA BUSCAR/ARMAZENAR
                string telefoneCriptografado = _cryptoService.Encrypt(telefoneNormalizado);

                // Buscar ou criar usuário (BUSCA PELO TELEFONE CRIPTOGRAFADO)
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Telefone == telefoneCriptografado);

                if (usuario == null)
                {
                    usuario = new UsuarioModel
                    {
                        Nome = request.NomeCliente,
                        Telefone = telefoneCriptografado,  // 🔥 SALVAR CRIPTOGRAFADO

                    };
                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();
                }

                // Gerar código sequencial
                int codigoPedido = await ObterProximoCodigoPedidoAsync(estabelecimento.Id_Estabelecimento);

                var pedido = new PedidoModel
                {
                    Id_Usuario = usuario.Id_Usuario,
                    Id_Estabelecimento = estabelecimento.Id_Estabelecimento,
                    CodigoPedido = codigoPedido,
                    Status_Pedido = StatusPedido.EM_PREPARACAO,
                    FormaPagamento = request.FormaPagamento,
                    TipoPedido = request.TipoPedido ?? "CONSUMIR",
                    Valor_Total = request.ValorTotal,
                    Data_Pedido = GetBrasiliaTime(),
                    Itens = new List<PedidoItemModel>()
                };

                foreach (var item in request.Itens)
                {
                    pedido.Itens.Add(new PedidoItemModel
                    {
                        Id_Produto = item.IdProduto,
                        Quantidade = item.Quantidade,
                        Valor_Unitario = item.PrecoUnitario
                    });
                }

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // ========== BUSCAR CATEGORIAS DOS PRODUTOS ==========
                var itensComCategoria = new List<object>();
                foreach (var item in request.Itens)
                {
                    var produto = await _context.Produtos
                        .Include(p => p.Categoria)
                        .FirstOrDefaultAsync(p => p.Id_Produto == item.IdProduto);

                    itensComCategoria.Add(new
                    {
                        idProduto = item.IdProduto,
                        nomeProduto = produto?.Nome_Produto ?? item.NomeProduto,
                        quantidade = item.Quantidade,
                        precoUnitario = item.PrecoUnitario,
                        categoria = produto?.Categoria?.Nome_Categoria ?? "Outros"
                    });
                }

                // ========== NOTIFICAÇÕES SIGNALR ==========

                // 1. Notificar Administradores
                var horaBrasilia = GetBrasiliaTime();
                await _pedidoHub.Clients.Group("Admins").SendAsync("NovoPedido", new
                {
                    id = pedido.Id_Pedido,
                    pedidoId = pedido.Id_Pedido,
                    codigo = pedido.CodigoPedido,
                    cliente = request.NomeCliente,
                    clienteNome = request.NomeCliente,
                    telefone = request.TelefoneCliente,  // Telefone normal (não criptografado) para exibição
                    total = request.ValorTotal,
                    formaPagamento = request.FormaPagamento,
                    tipoPedido = request.TipoPedido == "ENTREGAR" ? "📦 Para levar" : "🍽️ Comer no local",
                    status = "EM_PREPARACAO",
                    Status_Pedido = "EM_PREPARACAO",
                    data = horaBrasilia.ToString("dd/MM/yyyy"),
                    hora = horaBrasilia.ToString("HH:mm"),
                    itens = itensComCategoria,
                    totalItens = request.Itens.Sum(i => i.Quantidade)
                });

                // 2. Notificar o Cliente
                await _pedidoHub.Clients.Group($"cliente_{telefoneNormalizado}").SendAsync("PedidoConfirmado", new
                {
                    pedidoId = pedido.Id_Pedido,
                    codigo = pedido.CodigoPedido,
                    mensagem = $"Seu pedido #{pedido.CodigoPedido} foi confirmado e está sendo preparado!",
                    status = "EM_PREPARACAO",
                    Status_Pedido = "EM_PREPARACAO"
                });

                return Json(new
                {
                    success = true,
                    message = $"Pedido #{pedido.CodigoPedido} realizado com sucesso!",
                    codigoPedido = pedido.CodigoPedido,
                    pedidoId = pedido.Id_Pedido
                });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterEstabelecimento()
        {
            try
            {
                var estabelecimento = await _context.Estabelecimentos.FirstOrDefaultAsync();

                if (estabelecimento == null)
                {
                    return Json(new { success = false, message = "Estabelecimento não encontrado" });
                }

                var estabelecimentoDTO = new EstabelecimentoSessionDTO
                {
                    Id_Estabelecimento = estabelecimento.Id_Estabelecimento,
                    Nome_Estabelecimento = estabelecimento.Nome_Estabelecimento,
                    WhatsApp = estabelecimento.WhatsApp ?? "",
                    Img_Estabelecimento = estabelecimento.Img_Estabelecimento ?? "",
                    MensagemHero = estabelecimento.MensagemHero ?? ""
                };

                HttpContext.Session.SetString("Estabelecimento", JsonSerializer.Serialize(estabelecimentoDTO));
                HttpContext.Session.SetInt32("EstabelecimentoId", estabelecimento.Id_Estabelecimento);

                return Json(new
                {
                    success = true,
                    nome = estabelecimento.Nome_Estabelecimento,
                    whatsapp = estabelecimento.WhatsApp ?? "",
                    imagem = estabelecimento.Img_Estabelecimento ?? "",
                    mensagemHero = estabelecimento.MensagemHero ?? ""
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<int> ObterProximoCodigoPedidoAsync(int idEstabelecimento)
        {
            var hoje = DateTime.Today;

            using var transaction = await _context.Database.BeginTransactionAsync();

            var sequencia = await _context.SequenciasPedido
                .Where(s => s.Data == hoje && s.Id_Estabelecimento == idEstabelecimento)
                .FirstOrDefaultAsync();

            int proximoNumero;
            if (sequencia == null)
            {
                sequencia = new SequenciaPedido
                {
                    Data = hoje,
                    Id_Estabelecimento = idEstabelecimento,
                    UltimoNumero = 1
                };
                _context.SequenciasPedido.Add(sequencia);
                proximoNumero = 1;
            }
            else
            {
                proximoNumero = sequencia.UltimoNumero + 1;
                if (proximoNumero > 9999)
                    throw new Exception("Limite diário de pedidos atingido (9999).");
                sequencia.UltimoNumero = proximoNumero;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return proximoNumero;
        }

        [HttpGet]
        public async Task<IActionResult> MeusPedidos(string telefone)
        {
            // Garantir que o estabelecimento está na sessão
            var estabelecimentoSessao = HttpContext.Session.GetString("Estabelecimento");

            if (string.IsNullOrEmpty(estabelecimentoSessao))
            {
                var estabelecimento = await _context.Estabelecimentos.FirstOrDefaultAsync();
                if (estabelecimento != null)
                {
                    var estabelecimentoDTO = new EstabelecimentoSessionDTO
                    {
                        Id_Estabelecimento = estabelecimento.Id_Estabelecimento,
                        Nome_Estabelecimento = estabelecimento.Nome_Estabelecimento,
                        WhatsApp = estabelecimento.WhatsApp ?? "",
                        Img_Estabelecimento = estabelecimento.Img_Estabelecimento ?? "",
                        MensagemHero = estabelecimento.MensagemHero ?? ""
                    };
                    HttpContext.Session.SetString("Estabelecimento", JsonSerializer.Serialize(estabelecimentoDTO));
                    HttpContext.Session.SetInt32("EstabelecimentoId", estabelecimento.Id_Estabelecimento);
                }
            }

            if (string.IsNullOrEmpty(telefone))
            {
                return View(new List<PedidoModel>());
            }

            string telefoneNormalizado = new string(telefone.Where(char.IsDigit).ToArray());

            // 🔥 CRIPTOGRAFAR O TELEFONE PARA BUSCAR NO BANCO
            string telefoneCriptografado = _cryptoService.Encrypt(telefoneNormalizado);

            var pedidos = await _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Include(p => p.Usuario)
                .Where(p => p.Usuario.Telefone == telefoneCriptografado)
                .OrderByDescending(p => p.Data_Pedido)
                .ToListAsync();

            ViewBag.Telefone = telefoneNormalizado;
            return View(pedidos);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPedidos(string telefone)
        {
            if (string.IsNullOrEmpty(telefone))
            {
                return Json(new { success = false, message = "Digite seu telefone" });
            }

            string telefoneNormalizado = new string(telefone.Where(char.IsDigit).ToArray());

            // 🔥 CRIPTOGRAFAR O TELEFONE PARA BUSCAR NO BANCO
            string telefoneCriptografado = _cryptoService.Encrypt(telefoneNormalizado);

            var pedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Where(p => p.Usuario.Telefone == telefoneCriptografado)
                .OrderByDescending(p => p.Data_Pedido)
                .Select(p => new
                {
                    id = p.Id_Pedido,
                    codigo = p.CodigoPedido,
                    status = p.Status_Pedido.ToString(),
                    valorTotal = p.Valor_Total ?? 0,
                    dataPedido = p.Data_Pedido,
                    formaPagamento = p.FormaPagamento ?? "Dinheiro",
                    tipoPedido = p.TipoPedido ?? "CONSUMIR",
                    totalItens = p.Itens.Sum(i => i.Quantidade),
                    itens = p.Itens.Select(i => new
                    {
                        quantidade = i.Quantidade,
                        precoUnitario = i.Valor_Unitario,
                        nomeProduto = i.Produto != null ? i.Produto.Nome_Produto : "Produto"
                    })
                })
                .ToListAsync();

            return Json(new { success = true, pedidos = pedidos });
        }
    }

    public class PedidoRequest
    {
        public string NomeCliente { get; set; }
        public string TelefoneCliente { get; set; }
        public string Observacoes { get; set; }
        public string FormaPagamento { get; set; }
        public string TipoPedido { get; set; }
        public decimal ValorTotal { get; set; }
        public List<PedidoItemRequest> Itens { get; set; }
    }

    public class PedidoItemRequest
    {
        public int IdProduto { get; set; }
        public string NomeProduto { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public string Categoria { get; set; } = string.Empty;
    }
}