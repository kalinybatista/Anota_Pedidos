using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Anota_Pedidos.Filters
{
    public class AuthFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Verificar se o admin está logado
            var adminId = context.HttpContext.Session.GetInt32("AdminId");

            // Rotas que não exigem autenticação
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // Permitir acesso sem login apenas para LoginController e rotas públicas
            if (controller == "Login" ||
                (controller == "Usuario" && action == "Cardapio") ||
                action == "Login" ||
                action == "Cadastrar" ||
                action == "RecuperarSenha")
            {
                return;
            }

            // Se não estiver logado, redirecionar para o login
            if (adminId == null || adminId == 0)
            {
                context.Result = new RedirectToActionResult("Login", "Login", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}