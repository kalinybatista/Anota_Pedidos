using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Anota_Pedidos.Models
{
    public class ProdutoModel
    {
        [Key]
        public int Id_Produto { get; set; }

        [Required]
        [StringLength(50)]
        public string Nome_Produto { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor_Produto { get; set; }

        [StringLength(600)]
        public string? Descricao_Produto { get; set; }

        [Required]
        public int Id_Categoria { get; set; }

        [StringLength(500)]
        public string? Img_Produto { get; set; }

        [NotMapped]
        public IFormFile? ImagemArquivo { get; set; }

        [ForeignKey("Id_Categoria")]
        public virtual CategoriaModel? Categoria { get; set; }

        [Required]
        public int Id_Estabelecimento { get; set; }

        public DateTime? DataOcultacao { get; internal set; }
        public bool Ativo { get; internal set; }
    }
}