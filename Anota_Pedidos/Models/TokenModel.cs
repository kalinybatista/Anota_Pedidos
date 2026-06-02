using System;
using System.ComponentModel.DataAnnotations;

namespace Anota_Pedidos.Models
{
    public class TokenModel
    {
        [Key]
        public int Id_Token { get; set; }

        [Required]
        [StringLength(150)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(200)]
        public string Token { get; set; }

        [Required]
        public DateTime Expiration { get; set; }
    }
}