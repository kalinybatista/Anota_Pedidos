public class TokenRecuperacaoModel
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }
    public DateTime Expiracao { get; set; }
    public bool Usado { get; set; }
    public DateTime DataCriacao { get; set; }

}