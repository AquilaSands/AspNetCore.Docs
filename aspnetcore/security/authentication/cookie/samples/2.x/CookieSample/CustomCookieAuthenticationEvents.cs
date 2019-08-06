using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CookieSample
{
    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private readonly ILogger _logger;
        public CustomCookieAuthenticationEvents(ILogger<CustomCookieAuthenticationEvents> logger)
        {
            _logger = logger;
        }

        public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var path = context.Request.Path;
            if (string.Equals(path, "/Contact", StringComparison.OrdinalIgnoreCase))
            {
                var id = Guid.NewGuid();
                _logger.LogWarning("Throwing exception with guid {Guid}", id);
                throw new CustomException(id); 
            }
            return Task.CompletedTask;
        }
    }
}
