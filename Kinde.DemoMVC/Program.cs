using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = Directory.GetCurrentDirectory() + "\\wwwroot"
});
builder.WebHost.UseIISIntegration();

// Add services to the container.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Scope.Add("email");
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            // Set optional parameters https://docs.kinde.com/developer-tools/about/using-kinde-without-an-sdk/
            if (context.Properties.Parameters.ContainsKey("register"))
            {
                context.ProtocolMessage.Prompt = "create";
            }
            return Task.CompletedTask;
        };
    });
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddRazorPages();

var app = builder.Build();
app.UseSession();

// Configure the HTTP request pipeline.
if (app.Environment.IsProduction())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();