using NewRich.Shared.Helpers;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Filters;
using NewRich.Vendedor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<DebeCambiarPasswordFilter>();
});
builder.Services.AddAuthentication(AuthCookieNames.Scheme)
    .AddCookie(AuthCookieNames.Scheme, options =>
    {
        options.LoginPath = "/Cuenta/Ingresar";
        options.AccessDeniedPath = "/Cuenta/Ingresar";
        options.ExpireTimeSpan = InactividadSesion.Limite;
        options.SlidingExpiration = true;
        options.Cookie.Name = "nr.vendedor.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();

var apiBase = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:8090/";
builder.Services.AddHttpClient<IVendedorApiClient, VendedorApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.AddSingleton<IVendedorSessionService, VendedorSessionService>();
builder.Services.AddScoped<MinutosInactividadSesion>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Cuenta/Ingresar");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseMiddleware<MinutosInactividadMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Inicio}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
