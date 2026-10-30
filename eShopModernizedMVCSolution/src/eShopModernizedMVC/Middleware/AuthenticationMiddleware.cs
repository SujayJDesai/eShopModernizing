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

namespace eShopModernizedMVC.Middleware
{
    public class AuthenticationMiddleware     {
RequestDelegate _next = null;        public AuthenticationMiddleware(RequestDelegate next)         {
_next = next;        }

public async Task Invoke(HttpContext context)
        {
            var identity = new ClaimsIdentity("cookies");
            identity.AddClaim(new Claim("iat", "1234"));
            context.User = new ClaimsPrincipal(identity);
            await _next.Invoke(context);
        }
    }
}