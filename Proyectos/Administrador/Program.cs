using NewRich.Admin.Constants;
using NewRich.Shared.Helpers;
using NewRich.Admin.Filters;
using NewRich.Admin.Hubs;
using NewRich.Admin.Services;
using NewRich.Admin.Services.Pda;
using NewRich.Admin.Services.Usb;

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
        options.Cookie.Name = "nr.admin.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

var apiBase = builder.Configuration["Api:BaseUrl"] ?? "https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/";
builder.Services.AddHttpClient<IAdminApiClient, AdminApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IAdminSessionService, AdminSessionService>();

// Registro guiado de PDA: adb corre en este mismo computador, que es donde el administrador
// conecta el dispositivo por USB.
builder.Services.Configure<OpcionesRegistroPda>(builder.Configuration.GetSection(OpcionesRegistroPda.Seccion));
builder.Services.AddSingleton<IAdb, AdbProceso>();
builder.Services.AddSingleton<IApkPda, ApkPdaEnDisco>();
builder.Services.AddSingleton<CandadoRegistroPda>();
builder.Services.AddScoped<IRegistroPdaService, RegistroPdaService>();
builder.Services.AddSingleton<IInventarioUsb, InventarioUsbWindows>();
builder.Services.AddSingleton<IPreparadorLlaveUsb, PreparadorLlaveUsbWindows>();
builder.Services.AddSingleton<ILectorLlaveUsb, LectorLlaveUsb>();
builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Cuenta/Ingresar");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHub<RegistroPdaHub>(UiTexts.HubRegistroPda);
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Inicio}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
