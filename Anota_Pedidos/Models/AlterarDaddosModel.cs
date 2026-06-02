namespace Anota_Pedidos.Models
{
    public class AlterarDaddosModel
    {
        public string Token { get; set; }

        //Relacionamentos

        public virtual AdminModel? Nome { get; set; }
        public virtual AdminModel? Email { get; set; }
        public virtual AdminModel? Senha { get; set; }
    }
}
