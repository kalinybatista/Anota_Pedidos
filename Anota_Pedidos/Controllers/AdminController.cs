using Anota_Pedidos.Data;
using Anota_Pedidos.Filters;
using Anota_Pedidos.Hubs;
using Anota_Pedidos.Models;
using Anota_Pedidos.Services;
using Anota_Pedidos.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anota_Pedidos.Controllers
{
    [RequireHttps]
    [ServiceFilter(typeof(AuthFilter))]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<PedidoHub> _pedidoHub;
        private readonly ICryptoService _cryptoService;  // 🔥 ADICIONAR

        public AdminController(
            IAdminService adminService,
            ApplicationDbContext context,
            IHubContext<PedidoHub> pedidoHub,
            ICryptoService cryptoService)  // 🔥 ADICIONAR PARÂMETRO
        {
            _adminService = adminService;
            _context = context;
            _pedidoHub = pedidoHub;
            _cryptoService = cryptoService;  // 🔥 ADICIONAR
        }

        // ==================== Views Principais ====================

        public IActionResult Perfil()
        {
            return View();
        }

        public IActionResult Vendas()
        {
            return View();
        }

        // 🔥 MÉTODO AUXILIAR PARA DESCRIPTOGRAFAR TELEFONE
        private string DescriptografarTelefone(string telefoneCriptografado)
        {
            if (string.IsNullOrEmpty(telefoneCriptografado))
                return "-";

            try
            {
                var telefoneDescriptografado = _cryptoService.Decrypt(telefoneCriptografado);

                // Formatar telefone (XX) XXXXX-XXXX
                if (telefoneDescriptografado.Length == 11)
                {
                    return Convert.ToUInt64(telefoneDescriptografado).ToString(@"\(00\) 00000\-0000");
                }
                else if (telefoneDescriptografado.Length == 10)
                {
                    return Convert.ToUInt64(telefoneDescriptografado).ToString(@"\(00\) 0000\-0000");
                }
                return telefoneDescriptografado;
            }
            catch
            {
                return telefoneCriptografado;
            }
        }

        // GET: Admin/Pedidos
        public async Task<IActionResult> Pedidos()
        {
            try
            {
                var categoriaEspetinhos = await _context.Categorias
                    .FirstOrDefaultAsync(c => c.Nome_Categoria == "Espetinhos");

                ViewBag.CategoriaEspetinhosId = categoriaEspetinhos?.Id_Categoria ?? 0;

                var pedidos = await _context.Pedidos
                    .Include(p => p.Itens)
                        .ThenInclude(i => i.Produto)
                        .ThenInclude(prod => prod.Categoria)
                    .Include(p => p.Usuario)
                    .Where(p => p.Status_Pedido == StatusPedido.EM_PREPARACAO)
                    .OrderBy(p => p.Data_Pedido)
                    .ToListAsync();

                return View(pedidos);
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao carregar pedidos: {ex.Message}";
                return View(new List<PedidoModel>());
            }
        }

        // GET: Admin/Receber
        public async Task<IActionResult> Receber()
        {
            try
            {
                var pedidos = await _context.Pedidos
                    .Include(p => p.Itens)
                        .ThenInclude(i => i.Produto)
                    .Include(p => p.Usuario)
                    .Where(p => p.Status_Pedido == StatusPedido.PRONTO)
                    .OrderBy(p => p.Data_Pedido)
                    .ToListAsync();

                // 🔥 DESCRIPTOGRAFAR TELEFONES PARA EXIBIÇÃO NA VIEW
                foreach (var pedido in pedidos)
                {
                    if (pedido.Usuario != null && !string.IsNullOrEmpty(pedido.Usuario.Telefone))
                    {
                        pedido.Usuario.Telefone = DescriptografarTelefone(pedido.Usuario.Telefone);
                    }
                }

                return View(pedidos);
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao carregar pedidos: {ex.Message}";
                return View(new List<PedidoModel>());
            }
        }

        // GET: Admin/ObterHistoricoPagamentos
        [HttpGet]
        public async Task<IActionResult> ObterHistoricoPagamentos()
        {
            try
            {
                var pedidos = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Where(p => p.Status_Pedido == StatusPedido.FINALIZADO &&
                                p.Data_Pedido.Date == DateTime.Today)
                    .OrderByDescending(p => p.Data_Pedido)
                    .Take(10)
                    .ToListAsync();

                var pedidosDTO = pedidos.Select(p => new
                {
                    id = p.Id_Pedido,
                    codigoPedido = p.CodigoPedido,
                    clienteNome = p.Usuario?.Nome ?? "Cliente",
                    valorTotal = p.Valor_Total ?? 0m,
                    hora = p.Data_Pedido.ToString("HH:mm"),
                    formaPagamento = p.FormaPagamento ?? "Dinheiro"
                });

                return Json(new { success = true, pedidos = pedidosDTO });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== Stats Actions ====================

        [HttpGet]
        public async Task<IActionResult> ObterStats()
        {
            try
            {
                var categorias = await _adminService.ObterTodasCategoriasAsync();
                var totalProdutos = await _adminService.ObterTotalProdutosAsync();
                var totalMaisVendidos = await _adminService.ObterTotalProdutosMaisVendidosAsync();

                return Json(new
                {
                    success = true,
                    totalCategorias = categorias != null ? categorias.Count() : 0,
                    totalProdutos,
                    totalMaisVendidos
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // 🔥 MÉTODO AUXILIAR PARA NORMALIZAR TELEFONE (APENAS NÚMEROS)
        private string NormalizarTelefone(string telefone)
        {
            if (string.IsNullOrEmpty(telefone))
                return "";

            // Remove tudo que não é número
            var apenasNumeros = new string(telefone.Where(char.IsDigit).ToArray());

            // Garantir que tem 11 dígitos (se tiver 10, adiciona 9 na frente?)
            if (apenasNumeros.Length == 10)
                return "9" + apenasNumeros;

            return apenasNumeros;
        }

        // ==================== Pedidos Actions ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarStatusPedido([FromBody] AtualizarStatusRequest request)
        {
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .FirstOrDefaultAsync(p => p.Id_Pedido == request.id);

                if (pedido == null)
                    return Json(new { success = false, message = "Pedido não encontrado" });

                StatusPedido novoStatus = request.status switch
                {
                    "PRONTO" => StatusPedido.PRONTO,
                    "FINALIZADO" => StatusPedido.FINALIZADO,
                    _ => StatusPedido.EM_PREPARACAO
                };

                pedido.Status_Pedido = novoStatus;

                if (!string.IsNullOrEmpty(request.formaPagamento))
                {
                    pedido.FormaPagamento = request.formaPagamento;
                }

                await _context.SaveChangesAsync();

                // 🔥 DESCRIPTOGRAFAR E NORMALIZAR O TELEFONE
                var telefoneDescriptografado = DescriptografarTelefone(pedido.Usuario?.Telefone ?? "");
                var telefoneNormalizado = NormalizarTelefone(telefoneDescriptografado);

                Console.WriteLine($"📞 Telefone descriptografado: {telefoneDescriptografado}");
                Console.WriteLine($"📞 Telefone normalizado: {telefoneNormalizado}");

                await _pedidoHub.Clients.Group("Admins").SendAsync("PedidosAtualizados");

                return Json(new { success = true, message = $"Pedido #{pedido.CodigoPedido} marcado como {ObterStatusTexto(novoStatus)}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/ObterPedidosEmPreparo (API para carregar pedidos via AJAX)
        [HttpGet]
        public async Task<IActionResult> ObterPedidosEmPreparo()
        {
            try
            {
                Console.WriteLine("===== OBTENDO PEDIDOS EM PREPARO =====");

                var pedidos = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.Itens)
                        .ThenInclude(i => i.Produto)
                            .ThenInclude(p => p.Categoria)
                    .Where(p => p.Status_Pedido == StatusPedido.EM_PREPARACAO)
                    .OrderBy(p => p.Data_Pedido)
                    .ToListAsync();

                Console.WriteLine($"📦 Pedidos encontrados: {pedidos.Count}");

                // Calcular produtos da churrasqueira (apenas espetinhos)
                var produtosChurrasqueira = new Dictionary<string, int>();

                foreach (var pedido in pedidos)
                {
                    Console.WriteLine($"  Pedido #{pedido.CodigoPedido} - Status: {pedido.Status_Pedido}");

                    if (pedido.Itens != null)
                    {
                        foreach (var item in pedido.Itens)
                        {
                            var categoriaNome = item.Produto?.Categoria?.Nome_Categoria ?? "SEM CATEGORIA";
                            var produtoNome = item.Produto?.Nome_Produto ?? "PRODUTO DESCONHECIDO";

                            Console.WriteLine($"    Item: {produtoNome} | Categoria: {categoriaNome} | Qtd: {item.Quantidade}");

                            if (categoriaNome == "Espetinhos")
                            {
                                if (produtosChurrasqueira.ContainsKey(produtoNome))
                                    produtosChurrasqueira[produtoNome] += item.Quantidade;
                                else
                                    produtosChurrasqueira[produtoNome] = item.Quantidade;
                            }
                        }
                    }
                }

                Console.WriteLine($"🔥 Produtos na churrasqueira: {produtosChurrasqueira.Count}");
                foreach (var prod in produtosChurrasqueira)
                {
                    Console.WriteLine($"  - {prod.Key}: {prod.Value} unidades");
                }

                // 🔥 DESCRIPTOGRAFAR TELEFONES PARA EXIBIÇÃO
                var pedidosDTO = pedidos.Select(p => new
                {
                    id = p.Id_Pedido,
                    codigoPedido = p.CodigoPedido,
                    clienteNome = p.Usuario?.Nome ?? "Cliente",
                    telefone = DescriptografarTelefone(p.Usuario?.Telefone ?? ""),
                    tipoPedido = p.TipoPedido ?? "CONSUMIR",
                    valorTotal = p.Valor_Total ?? 0,
                    hora = p.Data_Pedido.ToString("HH:mm"),
                    itens = p.Itens.Select(i => new
                    {
                        quantidade = i.Quantidade,
                        nomeProduto = i.Produto?.Nome_Produto ?? "Produto",
                        precoUnitario = i.Valor_Unitario,
                        total = (i.Valor_Unitario) * i.Quantidade
                    })
                });

                return Json(new
                {
                    success = true,
                    pedidos = pedidosDTO,
                    produtosChurrasqueira = produtosChurrasqueira,
                    totalPedidos = pedidos.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERRO: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/ObterPedidosProntos (API para carregar pedidos prontos via AJAX)
        [HttpGet]
        public async Task<IActionResult> ObterPedidosProntos()
        {
            try
            {
                var pedidos = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.Itens)
                        .ThenInclude(i => i.Produto)
                    .Where(p => p.Status_Pedido == StatusPedido.PRONTO)
                    .OrderBy(p => p.Data_Pedido)
                    .ToListAsync();

                // 🔥 DESCRIPTOGRAFAR TELEFONES PARA EXIBIÇÃO
                var pedidosDTO = pedidos.Select(p => new
                {
                    id = p.Id_Pedido,
                    codigoPedido = p.CodigoPedido,
                    clienteNome = p.Usuario?.Nome ?? "Cliente",
                    telefone = DescriptografarTelefone(p.Usuario?.Telefone ?? ""),
                    tipoPedido = p.TipoPedido ?? "CONSUMIR",
                    valorTotal = p.Valor_Total ?? 0m,
                    hora = p.Data_Pedido.ToString("HH:mm"),
                    formaPagamento = p.FormaPagamento ?? "Dinheiro",
                    itens = p.Itens.Select(i => new
                    {
                        quantidade = i.Quantidade,
                        nomeProduto = i.Produto?.Nome_Produto ?? "Produto",
                        total = (i.Valor_Unitario) * i.Quantidade
                    })
                });

                return Json(new
                {
                    success = true,
                    pedidos = pedidosDTO,
                    totalPedidos = pedidos.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/ObterTodosPedidos (para estatísticas)
        [HttpGet]
        public async Task<IActionResult> ObterTodosPedidos()
        {
            try
            {
                var pedidos = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.Itens)
                        .ThenInclude(i => i.Produto)
                    .OrderByDescending(p => p.Data_Pedido)
                    .Take(50)
                    .ToListAsync();

                var pedidosDTO = pedidos.Select(p => new
                {
                    id = p.Id_Pedido,
                    codigoPedido = p.CodigoPedido,
                    clienteNome = p.Usuario?.Nome ?? "Cliente",
                    valorTotal = p.Valor_Total ?? 0,
                    data = p.Data_Pedido.ToString("dd/MM/yyyy HH:mm"),
                    status = p.Status_Pedido.ToString(),
                    formaPagamento = p.FormaPagamento ?? "Dinheiro",
                    itens = p.Itens.Count()
                });

                return Json(new { success = true, pedidos = pedidosDTO, total = pedidos.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string ObterStatusTexto(StatusPedido status)
        {
            return status switch
            {
                StatusPedido.EM_PREPARACAO => "em preparação",
                StatusPedido.PRONTO => "pronto",
                _ => "finalizado"
            };
        }

        public class AtualizarStatusRequest
        {
            public int id { get; set; }
            public string status { get; set; }
            public string formaPagamento { get; set; }
        }
    }
}