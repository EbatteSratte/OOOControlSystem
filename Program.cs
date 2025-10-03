using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using OOOControlSystem;
using OOOControlSystem.Middleware;
using OOOControlSystem.Models;
using OOOControlSystem.Models.Enums;
using OOOControlSystem.Services;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddCors(o =>
{
    o.AddPolicy("Spa", p => p
        .WithOrigins("http://localhost:5173")
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        .WithHeaders("Authorization", "Content-Type")
        .DisallowCredentials());
});

builder.Logging.Configure(o =>
{
    o.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId |
        ActivityTrackingOptions.Baggage |
        ActivityTrackingOptions.Tags;
});


builder.Services.AddProblemDetails();
builder.Services.AddControllers();


var cs = builder.Configuration.GetConnectionString("DefaultConnection");
var dsb = new NpgsqlDataSourceBuilder(cs);

dsb.EnableDynamicJson();
var dataSource = dsb.Build();

builder.Services.AddDbContext<ApplicationContext>(opt => opt.UseNpgsql(dataSource));
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key не найден в конфигурации");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();
app.UseCors("Spa");
app.UseStaticFiles();
app.Use(async (ctx, next) =>
{
    var sw = Stopwatch.StartNew();
    try
    {
        await next();
        sw.Stop();

        var ip = ctx.Connection.RemoteIpAddress;
        if (ip != null)
        {
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            else if (ip.Equals(IPAddress.IPv6Loopback)) ip = IPAddress.Loopback;
        }

        var log = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("HttpRequest");
        log.LogInformation("HTTP {Method} {Path} from {ClientIp} => {Status} in {Elapsed} ms",
            ctx.Request.Method, ctx.Request.Path, ip?.ToString(), ctx.Response.StatusCode, sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        sw.Stop();
        var ip = ctx.Connection.RemoteIpAddress;
        if (ip != null)
        {
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            else if (ip.Equals(IPAddress.IPv6Loopback)) ip = IPAddress.Loopback;
        }

        var log = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("HttpRequest");
        log.LogError(ex, "HTTP {Method} {Path} from {ClientIp} crashed after {Elapsed} ms",
            ctx.Request.Method, ctx.Request.Path, ip?.ToString(), sw.ElapsedMilliseconds);
        throw;
    }
});
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    dbContext.Database.Migrate();
    const string seedEmail = "manager@example.com";
    var plainPassword = "manager";
    var exists = dbContext.Users.Any(u => u.Email == seedEmail);
    if (!exists)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        dbContext.Users.Add(new User
        {
            Email = seedEmail,
            PasswordHash = hash,
            FullName = "Manager",
            Role = UserRole.Manager,
            IsActive = true
        });
        dbContext.SaveChanges();
    }
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<TokenValidationMiddleware>();

app.Run();
