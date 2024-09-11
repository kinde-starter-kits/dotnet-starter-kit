using System.Diagnostics;
using System.Security.Claims;
using Kinde.Api.Models.User;
using Kinde.DemoMVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kinde.DemoMVC.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        return View("Index");
    }

    [Authorize]
    public async Task<IActionResult> UserDetails()
    {
        var user = HttpContext.User;

        return View("UserDetails", new UserDetailViewModel
        {
            UserDetail = new KindeUserDetail
            {
                Picture = user.FindFirstValue("picture"),
                GivenName = user.FindFirstValue("given_name"),
                FamilyName = user.FindFirstValue("family_name"),
                Email = user.FindFirstValue("email"),
                Id = user.FindFirstValue("sub")
            }
        });
    }

    [Authorize]
    public async Task<IActionResult> Login()
    {
        // Where to take the user after login.
        return RedirectToAction("Index");
    }

    public async Task Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        // Where to take the user after logout can be configured with setting `SignedOutRedirectUri`.
    }

    public async Task SignUp()
    {
        var properties = new AuthenticationProperties();
        properties.Parameters.Add("register", true);
        properties.RedirectUri = "/"; // Where to take the user after signup.

        await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}