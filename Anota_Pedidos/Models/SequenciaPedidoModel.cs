using System;

namespace Anota_Pedidos.Models
{
    public class SequenciaPedido
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public int Id_Estabelecimento { get; set; }
        public int UltimoNumero { get; set; }
    }
}