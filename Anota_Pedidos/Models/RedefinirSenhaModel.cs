using System.ComponentModel.DataAnnotations;

namespace Anota_Pedidos.Models
{
    public class RedefinirSenhaModel
    {
        //[Required]
        public string Token { get; set; }

        //[Required]
        //[MinLength(8)]
        public string NovaSenha { get; set; }

        //[Required]
        //[Compare("NovaSenha")]
        public string ConfirmarSenha { get; set; }
    }
}
