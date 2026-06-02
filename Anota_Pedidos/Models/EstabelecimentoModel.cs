using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Anota_Pedidos.Models
{
    public class EstabelecimentoModel
    {
        [Key]
        public int Id_Estabelecimento { get; set; }

        [Required]
        public int Id_Admin { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome_Estabelecimento { get; set; }

        [StringLength(20)]
        public string? WhatsApp { get; set; }  // <-- ADICIONAR CAMPO WHATSAPP

        [StringLength(500)]
        public string? Img_Estabelecimento { get; set; }

        [StringLength(400)]
        public string? MensagemHero { get; set; }

        public DateTime Data_Atualizacao { get; set; } = DateTime.Now;

        [NotMapped]
        public IFormFile? ImagemArquivo { get; set; }

        [ForeignKey("Id_Admin")]
        public virtual AdminModel? Admin { get; set; }

        public virtual ICollection<PedidoModel>? Pedidos { get; set; }
    }

    // DTO para serialização (evita ciclo de referência)
    public class EstabelecimentoSessionDTO
    {
        public int Id_Estabelecimento { get; set; }
        public string Nome_Estabelecimento { get; set; }
        public string WhatsApp { get; set; }
        public string Img_Estabelecimento { get; set; }
        public string MensagemHero { get; set; }
    }
}