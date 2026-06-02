using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Anota_Pedidos.Filters
{
    public class ValidateAntiForgeryTokenApiFilter : IAsyncActionFilter
    {
        private readonly IAntiforgery _antiforgery;

        public ValidateAntiForgeryTokenApiFilter(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Verificar se é uma requisição que precisa de validação
            var request = context.HttpContext.Request;

            if (request.Method == "POST" || request.Method == "PUT" || request.Method == "DELETE")
            {
                try
                {
                    // Extrair token do header
                    var token = request.Headers["X-CSRF-TOKEN"].FirstOrDefault();

                    if (string.IsNullOrEmpty(token))
                    {
                        context.Result = new UnauthorizedResult();
                        return;
                    }

                    // Validar token
                    await _antiforgery.ValidateRequestAsync(context.HttpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }
            }

            await next();
        }
    }
}