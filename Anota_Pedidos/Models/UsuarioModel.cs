using System.ComponentModel.DataAnnotations;

namespace Anota_Pedidos.Models
{
    public class UsuarioModel
    {
        [Key]
        public int Id_Usuario { get; set; }

        [Required]
        [StringLength(100)]
        public required string Nome { get; set; }

        [StringLength(20)]
        public string? Telefone { get; set; }

        public virtual ICollection<PedidoModel>? Pedidos { get; set; }
    }
}