using System.ComponentModel.DataAnnotations;

namespace Anota_Pedidos.Models
{
    public class RecuperarSenhaModel
    {
        //[Required(ErrorMessage = "Email é obrigatório")]
        //[EmailAddress(ErrorMessage = "Email inválido")]
        public required string Email { get; set; }
    }
}
