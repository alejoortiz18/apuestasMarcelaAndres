using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NewRich.Api.Filters;
using NewRich.Api.Hubs;
using NewRich.Api.Realtime;
using NewRich.Application;
using NewRich.Application.Abstractions;
using NewRich.Infrastructure;
using NewRich.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<INotificacionTiempoReal, SignalRNotificacionTiempoReal>();
builder.Services.AddControllers(options => options.Filters.Add<SesionYPasswordActionFilter>());
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "New Rich API",
        Version = "v1",
        Description = "Autenticación JWT. Use POST /api/auth/login, copie el token y pégalo en Authorize."
    });

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue el token JWT obtenido en /api/auth/login. No escriba la palabra Bearer."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(NotificacionesHub.Ruta))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
var origenes = CorsOrigenes.ConLoopback(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? ["http://localhost:5274"]);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Admin", policy =>
    {
        policy.WithOrigins(origenes)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "New Rich API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "New Rich API";
        options.EnablePersistAuthorization();
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Admin");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificacionesHub>(NotificacionesHub.Ruta).RequireCors("Admin");

app.Run();
