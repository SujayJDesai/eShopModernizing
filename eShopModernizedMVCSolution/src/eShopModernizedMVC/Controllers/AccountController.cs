using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace eShopModernizedMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        public async Task SignIn()
        {
            _logger.LogInformation("Now processing... AccountController.SignIn");
            // Send an OpenID Connect sign-in request.
            if (!(User.Identity?.IsAuthenticated == true))
            {
                await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme);
            }
        }

        public async Task SignOut()
        {
            _logger.LogInformation("Now processing... AccountController.SignOut");
            // Send an OpenID Connect sign-out request.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        }

        public async Task EndSession()
        {
            _logger.LogInformation("Now processing... AccountController.EndSession");
            // If AAD sends a single sign-out message to the app, end the user's session, but don't redirect to AAD for sign out.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}