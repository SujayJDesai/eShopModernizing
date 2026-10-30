using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace eShopModernizedWebForms.Middleware
{
    public class AuthenticationMiddleware     {
RequestDelegate _next = null;        public AuthenticationMiddleware(RequestDelegate next)         {
_next = next;        }

public async Task Invoke(HttpContext context)
        {
            var identity = new ClaimsIdentity("cookies");
            identity.AddClaim(new Claim("iat", "1234"));
            var principal = new ClaimsPrincipal();
            principal.AddIdentity(identity);
            context.User = principal;
            await _next.Invoke(context);
        }
    }
}