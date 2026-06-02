using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Anota_Pedidos.Models
{
    public class AdminModel
    {
        [Key]
        public int Id_Admin { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string? EmailCriptografado { get; set; }

        [Required]
        [StringLength(255)]
        public string Senha { get; set; }

        [StringLength(500)]
        public string? Img_Admin { get; set; }

        public DateTime Data_Cadastro { get; set; } = DateTime.Now;

        [NotMapped]
        public IFormFile? ImagemArquivo { get; set; }

        public virtual ICollection<EstabelecimentoModel>? Estabelecimentos { get; set; }
    }
}