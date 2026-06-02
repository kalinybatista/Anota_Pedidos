using Anota_Pedidos.Data;
using Anota_Pedidos.Filters;
using Anota_Pedidos.Hubs;
using Anota_Pedidos.Models;
using Anota_Pedidos.Services;
using Anota_Pedidos.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Anota_Pedidos.Controllers
{
    [RequireHttps]
    [ServiceFilter(typeof(AuthFilter))]
    public class EditarController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IEstabelecimentoService _estabelecimentoService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<PedidoHub> _pedidoHub;

        public EditarController(
            IAdminService adminService,
            IEstabelecimentoService estabelecimentoService,
            ApplicationDbContext context,
            IHubContext<PedidoHub> pedidoHub)
        {
            _adminService = adminService;
            _estabelecimentoService = estabelecimentoService;
            _context = context;
            _pedidoHub = pedidoHub;
        }

        // ==================== Views Principais ====================



        // GET: Admin/Visualizar
        public async Task<IActionResult> Visualizar()
        {
            try
            {
                var viewModel = await _adminService.ObterGerenciarCardapioAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao carregar cardápio: {ex.Message}";
                return View(new GerenciarCardapioViewModel());
            }
        }



        // GET: Editar/Editar
        public async Task<IActionResult> Editar()
        {
            try
            {
                var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

                if (estabelecimentoId == 0)
                {
                    TempData["Erro"] = "Estabelecimento não encontrado. Faça login novamente.";
                    return RedirectToAction("Login", "Login");
                }

                // Buscar TODAS as categorias do estabelecimento (incluindo ocultas)
                var categorias = await _context.Categorias
                    .Where(c => c.Id_Estabelecimento == estabelecimentoId)
                    .Include(c => c.Produtos.Where(p => p.Id_Estabelecimento == estabelecimentoId))
                    .OrderBy(c => c.Nome_Categoria)
                    .ToListAsync();

                var totalProdutos = await _context.Produtos
                    .CountAsync(p => p.Id_Estabelecimento == estabelecimentoId);

                var produtosAtivos = await _context.Produtos
                    .CountAsync(p => p.Id_Estabelecimento == estabelecimentoId && p.Ativo);

                var categoriasAtivas = await _context.Categorias
                    .CountAsync(c => c.Id_Estabelecimento == estabelecimentoId && c.Ativo);

                var viewModel = new GerenciarCardapioViewModel
                {
                    Categorias = categorias,
                    TotalCategorias = categorias.Count,
                    CategoriasAtivas = categoriasAtivas,
                    TotalProdutos = totalProdutos,
                    ProdutosAtivos = produtosAtivos,
                    TotalMaisVendidos = await ObterTotalProdutosMaisVendidosAsync(estabelecimentoId)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao carregar cardápio: {ex.Message}";
                return View(new GerenciarCardapioViewModel());
            }
        }

        private async Task<int> ObterTotalProdutosMaisVendidosAsync(int estabelecimentoId)
        {
            try
            {
                var produtosAgrupados = await _context.PedidoItens
                    .Include(pi => pi.Produto)
                    .Where(pi => pi.Produto != null && pi.Produto.Id_Estabelecimento == estabelecimentoId)
                    .GroupBy(item => item.Id_Produto)
                    .Select(g => new { ProdutoId = g.Key, QuantidadeTotal = g.Sum(item => item.Quantidade) })
                    .OrderByDescending(x => x.QuantidadeTotal)
                    .ToListAsync();

                return produtosAgrupados.Count;
            }
            catch
            {
                return 0;
            }
        }

        // ==================== Categoria Actions ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarCategoria([FromBody] CategoriaModel categoria)
        {
            try
            {
                if (categoria == null)
                    return Json(new { success = false, message = "Dados da categoria não fornecidos" });

                if (string.IsNullOrWhiteSpace(categoria.Nome_Categoria))
                    return Json(new { success = false, message = "O nome da categoria é obrigatório" });

                if (categoria.Nome_Categoria.Length > 50)
                    return Json(new { success = false, message = "O nome da categoria deve ter no máximo 50 caracteres" });

                var novaCategoria = await _adminService.AdicionarCategoriaAsync(categoria);

                return Json(new
                {
                    success = true,
                    message = "Categoria adicionada com sucesso!",
                    categoria = new
                    {
                        id = novaCategoria.Id_Categoria,
                        nome = novaCategoria.Nome_Categoria,
                        descricao = novaCategoria.Descricao_Categoria,
                        totalProdutos = 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarCategoria([FromBody] CategoriaModel categoria)
        {
            try
            {
                if (categoria == null)
                    return Json(new { success = false, message = "Dados da categoria não fornecidos" });

                if (categoria.Id_Categoria <= 0)
                    return Json(new { success = false, message = "ID da categoria inválido" });

                if (string.IsNullOrWhiteSpace(categoria.Nome_Categoria))
                    return Json(new { success = false, message = "O nome da categoria é obrigatório" });

                if (categoria.Nome_Categoria.Length > 50)
                    return Json(new { success = false, message = "O nome da categoria deve ter no máximo 50 caracteres" });

                await _adminService.AtualizarCategoriaAsync(categoria);

                return Json(new
                {
                    success = true,
                    message = "Categoria atualizada com sucesso!",
                    categoria = new
                    {
                        id = categoria.Id_Categoria,
                        nome = categoria.Nome_Categoria,
                        descricao = categoria.Descricao_Categoria
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterCategoria(int id)
        {
            try
            {
                if (id <= 0)
                    return Json(new { success = false, message = "ID inválido" });

                var categoria = await _adminService.ObterCategoriaPorIdAsync(id);
                if (categoria == null)
                    return Json(new { success = false, message = "Categoria não encontrada" });

                return Json(new
                {
                    success = true,
                    categoria = new
                    {
                        id = categoria.Id_Categoria,
                        nome = categoria.Nome_Categoria,
                        descricao = categoria.Descricao_Categoria ?? string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== Produto Actions ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarProduto([FromForm] ProdutoModel produto, IFormFile imagemArquivo)
        {
            try
            {
                if (produto == null)
                    return Json(new { success = false, message = "Dados do produto não fornecidos" });

                if (string.IsNullOrWhiteSpace(produto.Nome_Produto))
                    return Json(new { success = false, message = "O nome do produto é obrigatório" });

                if (produto.Valor_Produto <= 0)
                    return Json(new { success = false, message = "O valor do produto deve ser maior que zero" });

                if (produto.Id_Categoria <= 0)
                    return Json(new { success = false, message = "Selecione uma categoria" });

                var novoProduto = await _adminService.AdicionarProdutoAsync(produto, imagemArquivo);

                return Json(new
                {
                    success = true,
                    message = "Produto adicionado com sucesso!",
                    produto = new
                    {
                        id = novoProduto.Id_Produto,
                        nome = novoProduto.Nome_Produto,
                        descricao = novoProduto.Descricao_Produto ?? string.Empty,
                        valor = novoProduto.Valor_Produto.ToString("F2"),
                        idCategoria = novoProduto.Id_Categoria,
                        nomeCategoria = novoProduto.Categoria?.Nome_Categoria,
                        imagem = novoProduto.Img_Produto ?? string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarProduto([FromForm] ProdutoModel produto, IFormFile imagemArquivo)
        {
            try
            {
                if (produto == null)
                    return Json(new { success = false, message = "Dados do produto não fornecidos" });

                if (produto.Id_Produto <= 0)
                    return Json(new { success = false, message = "ID do produto inválido" });

                if (string.IsNullOrWhiteSpace(produto.Nome_Produto))
                    return Json(new { success = false, message = "O nome do produto é obrigatório" });

                if (produto.Valor_Produto <= 0)
                    return Json(new { success = false, message = "O valor do produto deve ser maior que zero" });

                if (produto.Id_Categoria <= 0)
                    return Json(new { success = false, message = "Selecione uma categoria" });

                await _adminService.AtualizarProdutoAsync(produto, imagemArquivo);

                return Json(new
                {
                    success = true,
                    message = "Produto atualizado com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpGet]
        public async Task<IActionResult> ObterProduto(int id)
        {
            try
            {
                if (id <= 0)
                    return Json(new { success = false, message = "ID inválido" });

                var produto = await _adminService.ObterProdutoPorIdAsync(id);
                if (produto == null)
                    return Json(new { success = false, message = "Produto não encontrado" });

                return Json(new
                {
                    success = true,
                    produto = new
                    {
                        id = produto.Id_Produto,
                        nome = produto.Nome_Produto,
                        descricao = produto.Descricao_Produto ?? string.Empty,
                        valor = produto.Valor_Produto.ToString("F2"),
                        idCategoria = produto.Id_Categoria,
                        imagem = produto.Img_Produto ?? string.Empty
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObterProdutosPorCategoria(int categoriaId)
        {
            try
            {
                if (categoriaId <= 0)
                    return Json(new { success = false, message = "ID da categoria inválido" });

                var produtos = await _adminService.ObterProdutosPorCategoriaAsync(categoriaId);

                var produtosJson = new List<object>();
                foreach (var p in produtos)
                {
                    produtosJson.Add(new
                    {
                        id = p.Id_Produto,
                        nome = p.Nome_Produto,
                        descricao = p.Descricao_Produto ?? string.Empty,
                        valor = p.Valor_Produto.ToString("F2"),
                        imagem = p.Img_Produto ?? string.Empty
                    });
                }

                return Json(new
                {
                    success = true,
                    produtos = produtosJson
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // ==================== Ações para Produtos ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OcultarProduto([FromBody] int id)
        {
            try
            {
                await _adminService.OcultarProdutoAsync(id);
                return Json(new
                {
                    success = true,
                    message = "✅ Produto ocultado do cardápio! O histórico de pedidos permanece intacto."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestaurarProduto([FromBody] int id)
        {
            try
            {
                await _adminService.RestaurarProdutoAsync(id);
                return Json(new
                {
                    success = true,
                    message = "✅ Produto restaurado ao cardápio!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==================== Ações para Categorias ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OcultarCategoria([FromBody] int id)
        {
            try
            {
                await _adminService.OcultarCategoriaAsync(id);
                return Json(new
                {
                    success = true,
                    message = "✅ Categoria ocultada do cardápio! Os produtos também foram ocultados."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestaurarCategoria([FromBody] int id)
        {
            try
            {
                await _adminService.RestaurarCategoriaAsync(id);
                return Json(new
                {
                    success = true,
                    message = "✅ Categoria restaurada ao cardápio!"
                });
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
                    totalProdutos = totalProdutos,
                    totalMaisVendidos = totalMaisVendidos
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}
