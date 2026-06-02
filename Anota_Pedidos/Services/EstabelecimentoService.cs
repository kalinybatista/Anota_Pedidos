using Microsoft.AspNetCore.Http;

namespace Anota_Pedidos.Services
{
    public interface IEstabelecimentoService
    {
        int GetEstabelecimentoId();
        bool IsAuthenticated();
    }

    public class EstabelecimentoService : IEstabelecimentoService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EstabelecimentoService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetEstabelecimentoId()
        {
            return _httpContextAccessor.HttpContext?.Session.GetInt32("EstabelecimentoId") ?? 0;
        }

        public bool IsAuthenticated()
        {
            var adminId = _httpContextAccessor.HttpContext?.Session.GetInt32("AdminId");
            return adminId.HasValue && adminId.Value > 0;
        }
    }
}