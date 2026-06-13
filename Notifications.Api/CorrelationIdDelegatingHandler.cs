using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.Api.Services
{
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null && httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                if (!request.Headers.Contains("X-Correlation-Id"))
                {
                    request.Headers.Add("X-Correlation-Id", correlationId.ToString());
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}