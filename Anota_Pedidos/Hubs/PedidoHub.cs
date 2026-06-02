using Microsoft.AspNetCore.SignalR;


namespace Anota_Pedidos.Hubs
{
    public class PedidoHub : Hub
    {
        // ==================== ADMIN ====================
        public async Task EntrarGrupoAdmin()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            Console.WriteLine($"👑 Admin entrou no grupo: {Context.ConnectionId}");
        }

        public async Task SairGrupoAdmin()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
            Console.WriteLine($"👑 Admin saiu do grupo: {Context.ConnectionId}");
        }

        // ==================== CLIENTE ====================
        public async Task EntrarGrupoCliente(string telefone)
        {
            if (!string.IsNullOrEmpty(telefone))
            {
                var grupoCliente = telefone.Length == 11 ? telefone : new string(telefone.Where(char.IsDigit).ToArray());
                await Groups.AddToGroupAsync(Context.ConnectionId, $"cliente_{grupoCliente}");
                Console.WriteLine($"📱 Cliente {grupoCliente} entrou no grupo cliente_{grupoCliente}");
            }
        }

        public async Task SairGrupoCliente(string telefone)
        {
            if (!string.IsNullOrEmpty(telefone))
            {
                var grupoCliente = telefone.Length == 11 ? telefone : new string(telefone.Where(char.IsDigit).ToArray());
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"cliente_{grupoCliente}");
                Console.WriteLine($"📱 Cliente {grupoCliente} saiu do grupo");
            }
        }

        // ==================== NOTIFICAÇÕES ====================

        /// <summary>
        /// Notifica admins que a lista de pedidos foi atualizada
        /// </summary>
        public async Task NotificarPedidosAtualizados()
        {
            await Clients.Group("Admins").SendAsync("PedidosAtualizados");
            Console.WriteLine($"🔄 Pedidos atualizados notificado para admins");
        }


        // ==================== MÉTODOS LEGADO (para compatibilidade) ====================
        public async Task NotificarNovoPedido(int pedidoId, string nomeCliente, decimal valorTotal, string formaPagamento)
        {
            var pedido = new
            {
                id = pedidoId,
                cliente = nomeCliente,
                total = valorTotal,
                formaPagamento,
                data = DateTime.Now.ToString("HH:mm")
            };
            await Clients.Group("Admins").SendAsync("NovoPedido", pedido);
        }

        public async Task NotificarPedidoPronto(int pedidoId, string nomeCliente)
        {
            var data = new
            {
                id = pedidoId,
                cliente = nomeCliente,
                mensagem = $"Pedido #{pedidoId} - {nomeCliente} está pronto!"
            };
            await Clients.Group("Admins").SendAsync("PedidoPronto", data);
        }

        public async Task NotificarAtualizarPedidos()
        {
            await Clients.Group("Admins").SendAsync("PedidosAtualizados");
        }

        // ==================== CONEXÃO ====================
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"✅ Cliente conectado: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"❌ Cliente desconectado: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}