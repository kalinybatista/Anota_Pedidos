using Anota_Pedidos.Data;
using Anota_Pedidos.Models;
using Anota_Pedidos.Repository;
using Anota_Pedidos.Services;
using Anota_Pedidos.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Anota_Pedidos.Services
{
    public class AdminService : IAdminService
    {
        private readonly CategoriaRepository _categoriaRepository;
        private readonly ApplicationDbContext _context;
        private readonly IEstabelecimentoService _estabelecimentoService;
        private readonly IRepository<ProdutoModel> _produtoRepository;
        private readonly IRepository<PedidoItemModel> _pedidoItemRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminService(
            CategoriaRepository categoriaRepository,
            ApplicationDbContext context,
            IEstabelecimentoService estabelecimentoService,
            IRepository<ProdutoModel> produtoRepository,
            IRepository<PedidoItemModel> pedidoItemRepository,
            IWebHostEnvironment webHostEnvironment)
        {
            _categoriaRepository = categoriaRepository;
            _context = context;
            _estabelecimentoService = estabelecimentoService;
            _produtoRepository = produtoRepository;
            _pedidoItemRepository = pedidoItemRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==================== Categoria Methods ====================

        public async Task<GerenciarCardapioViewModel> ObterGerenciarCardapioAsync()
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            // Para o cardápio do cliente: apenas categorias e produtos ativos
            var categorias = await _context.Categorias
                .Where(c => c.Id_Estabelecimento == estabelecimentoId && c.Ativo)
                .Include(c => c.Produtos.Where(p => p.Id_Estabelecimento == estabelecimentoId && p.Ativo))
                .OrderBy(c => c.Nome_Categoria)
                .ToListAsync();

            // Para o admin: mostrar tudo (incluindo ocultos) - use outro método específico
            var totalProdutos = await _context.Produtos
                .CountAsync(p => p.Id_Estabelecimento == estabelecimentoId);

            var totalMaisVendidos = await ObterTotalProdutosMaisVendidosAsync();

            return new GerenciarCardapioViewModel
            {
                Categorias = categorias,
                TotalCategorias = categorias.Count,
                TotalProdutos = totalProdutos,
                TotalMaisVendidos = totalMaisVendidos
            };
        }

        // Para a tela de edição do ADMIN - mostrar todos (incluindo ocultos)
        public async Task<GerenciarCardapioViewModel> ObterGerenciarCardapioAdminAsync()
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            // Mostrar TODAS as categorias (incluindo ocultas)
            var categorias = await _context.Categorias
                .Where(c => c.Id_Estabelecimento == estabelecimentoId)
                .Include(c => c.Produtos.Where(p => p.Id_Estabelecimento == estabelecimentoId)) // Todos os produtos
                .OrderBy(c => c.Nome_Categoria)
                .ToListAsync();

            var totalProdutos = await _context.Produtos
                .CountAsync(p => p.Id_Estabelecimento == estabelecimentoId);
    
            var totalProdutosAtivos = await _context.Produtos
                .CountAsync(p => p.Id_Estabelecimento == estabelecimentoId && p.Ativo);
    
            var totalCategoriasAtivas = await _context.Categorias
                .CountAsync(c => c.Id_Estabelecimento == estabelecimentoId && c.Ativo);

            return new GerenciarCardapioViewModel
            {
                Categorias = categorias,
                TotalCategorias = categorias.Count,
                TotalCategoriasAtivas = totalCategoriasAtivas,
                TotalProdutos = totalProdutos,
                TotalProdutosAtivos = totalProdutosAtivos,
                TotalMaisVendidos = await ObterTotalProdutosMaisVendidosAsync()
            };
        }

        public async Task<CategoriaModel?> ObterCategoriaPorIdAsync(int id)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id_Categoria == id && c.Id_Estabelecimento == estabelecimentoId);
        }

        public async Task<CategoriaModel?> ObterCategoriaComProdutosPorIdAsync(int id)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Categorias
                .Include(c => c.Produtos)
                .FirstOrDefaultAsync(c => c.Id_Categoria == id && c.Id_Estabelecimento == estabelecimentoId);
        }

        public async Task<IEnumerable<CategoriaModel>> ObterTodasCategoriasAsync()
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Categorias
                .Where(c => c.Id_Estabelecimento == estabelecimentoId)
                .Include(c => c.Produtos)
                .OrderBy(c => c.Nome_Categoria)
                .ToListAsync();
        }

        public async Task<CategoriaModel> AdicionarCategoriaAsync(CategoriaModel categoria)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            if (estabelecimentoId == 0)
            {
                throw new Exception("Estabelecimento não encontrado. Faça login novamente.");
            }

            // 🔥 VALIDAÇÃO: Verificar se já existe categoria com este nome
            var existe = await _context.Categorias
                .AnyAsync(c => c.Nome_Categoria.ToLower() == categoria.Nome_Categoria.ToLower()
                            && c.Id_Estabelecimento == estabelecimentoId);

            if (existe)
            {
                throw new Exception("Já existe uma categoria com este nome");
            }

            // 🔥 CRIAR A CATEGORIA MANUALMENTE
            var novaCategoria = new CategoriaModel
            {
                Nome_Categoria = categoria.Nome_Categoria.Trim(),
                Descricao_Categoria = categoria.Descricao_Categoria?.Trim(),
                Id_Estabelecimento = estabelecimentoId,
                Ativo = true,
                DataOcultacao = null
            };

            // 🔥 USAR O CONTEXT DIRETAMENTE (evitar problemas com repositório)
            _context.Categorias.Add(novaCategoria);
            await _context.SaveChangesAsync();

            return novaCategoria;
        }

        public async Task AtualizarCategoriaAsync(CategoriaModel categoria)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            var categoriaExistente = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id_Categoria == categoria.Id_Categoria && c.Id_Estabelecimento == estabelecimentoId);

            if (categoriaExistente == null)
            {
                throw new Exception("Categoria não encontrada");
            }

            if (categoriaExistente.Nome_Categoria != categoria.Nome_Categoria &&
                await CategoriaExisteAsync(categoria.Nome_Categoria))
            {
                throw new Exception("Já existe uma categoria com este nome");
            }

            categoriaExistente.Nome_Categoria = categoria.Nome_Categoria;
            categoriaExistente.Descricao_Categoria = categoria.Descricao_Categoria;

            await _context.SaveChangesAsync();
        }


        public async Task<bool> CategoriaExisteAsync(string nome)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Categorias
                .AnyAsync(c => c.Nome_Categoria.ToLower() == nome.ToLower() && c.Id_Estabelecimento == estabelecimentoId);
        }

        // ==================== Produto Methods ====================

        public async Task<ProdutoModel?> ObterProdutoPorIdAsync(int id)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Produtos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id_Produto == id && p.Id_Estabelecimento == estabelecimentoId);
        }

        public async Task<IEnumerable<ProdutoModel>> ObterTodosProdutosAsync()
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Produtos
                .Where(p => p.Id_Estabelecimento == estabelecimentoId)
                .Include(p => p.Categoria)
                .OrderBy(p => p.Categoria.Nome_Categoria)
                .ThenBy(p => p.Nome_Produto)
                .ToListAsync();
        }

        public async Task<IEnumerable<ProdutoModel>> ObterProdutosPorCategoriaAsync(int categoriaId)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Produtos
                .Where(p => p.Id_Categoria == categoriaId && p.Id_Estabelecimento == estabelecimentoId)
                .Include(p => p.Categoria)
                .OrderBy(p => p.Nome_Produto)
                .ToListAsync();
        }

        public async Task<ProdutoModel> AdicionarProdutoAsync(ProdutoModel produto, IFormFile? imagemArquivo)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();
            produto.Id_Estabelecimento = estabelecimentoId;
            produto.Ativo = true;  // 🔥 GARANTIR QUE VEM ATIVO (VISÍVEL)
            produto.DataOcultacao = null;  // 🔥 GARANTIR QUE NÃO TEM DATA DE OCULTAÇÃO


            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id_Categoria == produto.Id_Categoria && c.Id_Estabelecimento == estabelecimentoId);

            if (categoria == null)
            {
                throw new Exception("Categoria não encontrada");
            }

            if (await ProdutoExisteNaCategoriaAsync(produto.Id_Categoria, produto.Nome_Produto))
            {
                throw new Exception($"Já existe um produto com o nome '{produto.Nome_Produto}' nesta categoria");
            }

            if (imagemArquivo != null && imagemArquivo.Length > 0)
            {
                produto.Img_Produto = await SalvarImagemAsync(imagemArquivo, "produtos");
            }

            var novoProduto = await _produtoRepository.AddAsync(produto);
            return novoProduto;
        }

        public async Task AtualizarProdutoAsync(ProdutoModel produto, IFormFile? imagemArquivo)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            var produtoExistente = await _context.Produtos
                .FirstOrDefaultAsync(p => p.Id_Produto == produto.Id_Produto && p.Id_Estabelecimento == estabelecimentoId);

            if (produtoExistente == null)
                throw new Exception("Produto não encontrado");

            produtoExistente.Nome_Produto = produto.Nome_Produto;
            produtoExistente.Descricao_Produto = produto.Descricao_Produto;
            produtoExistente.Valor_Produto = produto.Valor_Produto;
            produtoExistente.Id_Categoria = produto.Id_Categoria;

            if (imagemArquivo != null && imagemArquivo.Length > 0)
            {
                if (!string.IsNullOrEmpty(produtoExistente.Img_Produto))
                    RemoverImagem(produtoExistente.Img_Produto);
                produtoExistente.Img_Produto = await SalvarImagemAsync(imagemArquivo, "produtos");
            }

            await _context.SaveChangesAsync();
        }



        public async Task<bool> ProdutoExisteNaCategoriaAsync(int categoriaId, string nomeProduto)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Produtos
                .AnyAsync(p => p.Id_Categoria == categoriaId &&
                              p.Nome_Produto.ToLower() == nomeProduto.ToLower() &&
                              p.Id_Estabelecimento == estabelecimentoId);
        }

        // ==================== Stats Methods ====================

        public async Task<int> ObterTotalProdutosAsync()
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            return await _context.Produtos
                .CountAsync(p => p.Id_Estabelecimento == estabelecimentoId);
        }

        public async Task<int> ObterTotalProdutosMaisVendidosAsync()
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

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
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao calcular produtos mais vendidos: {ex.Message}");
                return 0;
            }
        }

        // ==================== Helper Methods ====================

        private async Task<string?> SalvarImagemAsync(IFormFile arquivo, string pasta)
        {
            if (arquivo == null || arquivo.Length == 0)
                return null;

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", pasta);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string extensao = Path.GetExtension(arquivo.FileName);
            string nomeUnico = $"{Guid.NewGuid()}{extensao}";
            string caminhoArquivo = Path.Combine(uploadsFolder, nomeUnico);

            using (var fileStream = new FileStream(caminhoArquivo, FileMode.Create))
            {
                await arquivo.CopyToAsync(fileStream);
            }

            return $"/uploads/{pasta}/{nomeUnico}";
        }

        private void RemoverImagem(string caminhoImagem)
        {
            if (string.IsNullOrEmpty(caminhoImagem))
                return;

            string caminhoCompleto = Path.Combine(_webHostEnvironment.WebRootPath, caminhoImagem.TrimStart('/'));

            if (File.Exists(caminhoCompleto))
            {
                File.Delete(caminhoCompleto);
            }
        }

        // Ocultar produto (Soft Delete)
        public async Task<bool> OcultarProdutoAsync(int id)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.Id_Produto == id && p.Id_Estabelecimento == estabelecimentoId);

            if (produto == null)
                throw new Exception("Produto não encontrado");

            produto.Ativo = false;
            produto.DataOcultacao = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // Restaurar produto (tornar visível novamente)
        public async Task<bool> RestaurarProdutoAsync(int id)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(p => p.Id_Produto == id && p.Id_Estabelecimento == estabelecimentoId);

            if (produto == null)
                throw new Exception("Produto não encontrado");

            produto.Ativo = true;
            produto.DataOcultacao = null;
            await _context.SaveChangesAsync();
            return true;
        }

        // Ocultar categoria
        public async Task<bool> OcultarCategoriaAsync(int id)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            var categoria = await _context.Categorias
                .Include(c => c.Produtos)
                .FirstOrDefaultAsync(c => c.Id_Categoria == id && c.Id_Estabelecimento == estabelecimentoId);

            if (categoria == null)
                throw new Exception("Categoria não encontrada");

            // Ocultar todos os produtos da categoria
            foreach (var produto in categoria.Produtos)
            {
                produto.Ativo = false;
                produto.DataOcultacao = DateTime.Now;
            }

            categoria.Ativo = false;
            categoria.DataOcultacao = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // Restaurar categoria
        public async Task<bool> RestaurarCategoriaAsync(int id)
        {
            var estabelecimentoId = _estabelecimentoService.GetEstabelecimentoId();

            var categoria = await _context.Categorias
                .Include(c => c.Produtos)
                .FirstOrDefaultAsync(c => c.Id_Categoria == id && c.Id_Estabelecimento == estabelecimentoId);

            if (categoria == null)
                throw new Exception("Categoria não encontrada");

            categoria.Ativo = true;
            categoria.DataOcultacao = null;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}