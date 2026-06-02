using Microsoft.AspNetCore.Mvc;
using Anota_Pedidos.Data;
using Microsoft.EntityFrameworkCore;

namespace Anota_Pedidos.Controllers
{
    public class TesteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TesteController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            try
            {
                // Tenta conectar ao banco
                var canConnect = _context.Database.CanConnect();

                if (canConnect)
                {
                    ViewBag.Mensagem = "✅ Conexão com o MySQL estabelecida com sucesso!";
                }
                else
                {
                    ViewBag.Mensagem = "❌ Não foi possível conectar ao MySQL.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Mensagem = $"❌ Erro de conexão: {ex.Message}";
            }

            return View();
        }
    }
}
