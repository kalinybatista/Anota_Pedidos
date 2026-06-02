using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Anota_Pedidos.Models
{
    public class CategoriaModel
    {
        [Key]
        public int Id_Categoria { get; set; }

        [Required]
        [ForeignKey("Estabelecimento")]
        public int Id_Estabelecimento { get; set; }

        [Required]
        [StringLength(50)]
        public string Nome_Categoria { get; set; }

        [StringLength(600)]
        public string? Descricao_Categoria { get; set; }

        public virtual EstabelecimentoModel? Estabelecimento { get; set; }

        public virtual ICollection<ProdutoModel>? Produtos { get; set; }

        public bool Ativo { get; set; } = true;
        public DateTime? DataOcultacao { get; set; }
    }
}