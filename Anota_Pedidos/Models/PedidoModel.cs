using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Anota_Pedidos.Models
{
    public enum StatusPedido
    {
        EM_PREPARACAO,
        PRONTO,
        FINALIZADO
    }

    public class PedidoModel
    {
        [Key]
        public int Id_Pedido { get; set; }

        [Required]
        public int Id_Usuario { get; set; }

        [Required]
        public int Id_Estabelecimento { get; set; }

        [Required]
        public int CodigoPedido { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public StatusPedido Status_Pedido { get; set; } = StatusPedido.EM_PREPARACAO;

        [StringLength(20)]
        public string? TipoPedido { get; set; }

        [StringLength(50)]
        public string? FormaPagamento { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Valor_Total { get; set; }

        public DateTime Data_Pedido { get; set; } = DateTime.Now;

        // Relacionamentos
        [ForeignKey("Id_Usuario")]
        public virtual UsuarioModel? Usuario { get; set; }

        [ForeignKey("Id_Estabelecimento")]
        public virtual EstabelecimentoModel? Estabelecimento { get; set; }

        public virtual ICollection<PedidoItemModel>? Itens { get; set; }

    }
}