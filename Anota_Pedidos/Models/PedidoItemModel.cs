using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Anota_Pedidos.Models
{
    public class PedidoItemModel
    {
        [Key]
        public int Id_Item { get; set; }

        [Required]
        public int Id_Pedido { get; set; }

        [Required]
        public int Id_Produto { get; set; }
        [Required]
        public int Quantidade { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Valor_Unitario { get; set; }

        // Relacionamentos
        [ForeignKey("Id_Pedido")]
        public virtual PedidoModel? Pedido { get; set; }

        [ForeignKey("Id_Produto")]
        public virtual ProdutoModel? Produto { get; set; }
    }
}