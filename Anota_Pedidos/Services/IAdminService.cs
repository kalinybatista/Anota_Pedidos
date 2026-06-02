using Anota_Pedidos.Models;
using Anota_Pedidos.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anota_Pedidos.Services
{
    public interface IAdminService
    {
        // Categoria methods
        Task<GerenciarCardapioViewModel> ObterGerenciarCardapioAsync();    
        Task<GerenciarCardapioViewModel> ObterGerenciarCardapioAdminAsync();
        Task<CategoriaModel?> ObterCategoriaPorIdAsync(int id);
        Task<CategoriaModel?> ObterCategoriaComProdutosPorIdAsync(int id);
        Task<IEnumerable<CategoriaModel>> ObterTodasCategoriasAsync();
        Task<CategoriaModel> AdicionarCategoriaAsync(CategoriaModel categoria);
        Task AtualizarCategoriaAsync(CategoriaModel categoria);
        Task<bool> CategoriaExisteAsync(string nome);

        // Produto methods
        Task<ProdutoModel?> ObterProdutoPorIdAsync(int id);
        Task<IEnumerable<ProdutoModel>> ObterTodosProdutosAsync();
        Task<IEnumerable<ProdutoModel>> ObterProdutosPorCategoriaAsync(int categoriaId);
        Task<ProdutoModel> AdicionarProdutoAsync(ProdutoModel produto, IFormFile? imagemArquivo);
        Task AtualizarProdutoAsync(ProdutoModel produto, IFormFile? imagemArquivo);
        Task<bool> ProdutoExisteNaCategoriaAsync(int categoriaId, string nomeProduto);

        // Soft Delete / Ocultar methods
        Task<bool> OcultarProdutoAsync(int id);
        Task<bool> RestaurarProdutoAsync(int id);
        Task<bool> OcultarCategoriaAsync(int id);
        Task<bool> RestaurarCategoriaAsync(int id);

        // Stats
        Task<int> ObterTotalProdutosAsync();
        Task<int> ObterTotalProdutosMaisVendidosAsync();
 
    }
}