using Anota_Pedidos.Models;
using System.Collections.Generic;

namespace Anota_Pedidos.ViewModels
{
    public class GerenciarCardapioViewModel
    {
        public List<CategoriaModel> Categorias { get; set; } = new();
        public int TotalCategorias { get; set; }
        public int TotalCategoriasAtivas { get; set; }
        public int TotalProdutos { get; set; }
        public int TotalProdutosAtivos { get; set; }
        public int TotalMaisVendidos { get; set; }


        // Propriedades para ativos
        public int ProdutosAtivos { get; set; }
        public int CategoriasAtivas { get; set; }
    }
}