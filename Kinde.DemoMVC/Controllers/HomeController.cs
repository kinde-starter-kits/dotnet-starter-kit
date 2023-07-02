using Kinde.Api;
using Kinde.Api.Api;
using Kinde.Api.Client;
using Kinde.Api.Enums;
using Kinde.Api.Models.Configuration;
using Kinde.Api.Models.User;
using Kinde.DemoMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;

namespace Kinde.DemoMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAuthorizationConfigurationProvider _authConfigurationProvider;
        private readonly IApplicationConfigurationProvider _appConfigurationProvider;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IAuthorizationConfigurationProvider authConfigurationProvider, IApplicationConfigurationProvider appConfigurationProvider)
        {
            _logger = logger;
            _authConfigurationProvider = authConfigurationProvider;
            _appConfigurationProvider = appConfigurationProvider;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var correlationId = HttpContext.Session.GetString("KindeCorrelationId");
            if (!string.IsNullOrEmpty(correlationId))
            {
                var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
                if (client.AuthorizationState == AuthorizationStates.Authorized)
                {
                    ViewBag.Authorized = true;

                    // example of using oauth mgmt api to get user profile data
                    var oauthApi = new OAuthApi(client);
                    var user = await oauthApi.GetUserProfileV2Async();

                    return View("Index", user);
                }
            }
            return View("Index");
        }

        public async Task<IActionResult> UserDetails()
        {
            var correlationId = HttpContext.Session.GetString("KindeCorrelationId");
            if (!string.IsNullOrEmpty(correlationId))
            {
                var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
                if (client.AuthorizationState == AuthorizationStates.Authorized)
                {
                    ViewBag.Authorized = true;

                    return View("UserDetails", new UserDetailViewModel
                    {
                        UserDetail = client.GetUserDetails(),
                        Token = await client.GetToken()
                    });
                }
            }
            return RedirectToAction("Index");
        }

        public IActionResult HelperFunctions()
        {
            var correlationId = HttpContext.Session.GetString("KindeCorrelationId");
            if (!string.IsNullOrEmpty(correlationId))
            {
                var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
                if (client.AuthorizationState == AuthorizationStates.Authorized)
                {
                    ViewBag.Authorized = true;
                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };

                    var model = new HelperFunctionsViewModel
                    {
                        UserDetail = client.GetUserDetails(),
                        IssClaim = JsonConvert.SerializeObject(client.GetClaim("iss"), settings),
                        Organization = client.GetOrganization() ?? "",
                        UserOrganizations = JsonConvert.SerializeObject(client.GetOrganizations(), settings),
                        ThemeFlag = JsonConvert.SerializeObject(client.GetFlag("theme", new FeatureFlagValue { DefaultValue = "white" }), settings),
                        IsDarkMode = client.GetBooleanFlag("is_dark_mode", true)?.ToString() ?? "",
                        Theme = client.GetStringFlag("theme", "white"),
                        CompetitionsLimit = client.GetIntegerFlag("competitions_limit", 10)
                    };
                    _logger.LogInformation(JsonConvert.SerializeObject(model, settings));
                    return View("HelperFunctions", model);
                }
            }
            return RedirectToAction("Index");
        }

        public IActionResult Callback(string code, string state)
        {
            KindeClient.OnCodeReceived(code, state);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Login()
        {
            // We need some artificial id to correlate user session to client instance
            // NOTE: Session.Id will be always random, we need to add something to session to make it persistent. 
            var correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                HttpContext.Session?.SetString("KindeCorrelationId", correlationId);
            }

            // Get client's instance...
            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());

            // ...and authorize it
            await client.Authorize(_authConfigurationProvider.Get());

            // if auth flow is not ClientCredentials flow, we need to redirect user to another page
            if (client.AuthorizationState == AuthorizationStates.UserActionsNeeded)
            {
                // redirect user to login page
                return Redirect(await client.GetRedirectionUrl(correlationId));
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Renew()
        {
            var correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
            if (string.IsNullOrEmpty(correlationId))
            {
                return RedirectToAction("Index");
            }

            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
            await client.Renew();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Logout()
        {
            var correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
            if (string.IsNullOrEmpty(correlationId))
            {
                return RedirectToAction("Index");
            }

            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
            var url = await client.Logout();

            return Redirect(url);
        }

        public async Task<IActionResult> SignUp()
        {
            var correlationId = HttpContext.Session?.GetString("KindeCorrelationId");
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                HttpContext.Session?.SetString("KindeCorrelationId", correlationId);
            }

            var client = KindeClientFactory.Instance.GetOrCreate(correlationId, _appConfigurationProvider.Get());
            await client.Register(_authConfigurationProvider.Get());
            if (client.AuthorizationState == AuthorizationStates.UserActionsNeeded)
            {
                return Redirect(await client.GetRedirectionUrl(correlationId));
            }

            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}