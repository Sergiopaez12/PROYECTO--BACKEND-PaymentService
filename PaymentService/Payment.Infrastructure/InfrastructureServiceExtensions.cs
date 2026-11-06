using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Persistencia con SQLite
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=payment.db"));

        // Repositorios y Servicios
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentProcessor, MockPaymentProcessor>();

        // Cliente HTTP para consultar órdenes en el microservicio ECommerce
        services.AddHttpContextAccessor();
        var ecommerceBaseUrl = configuration["Services:ECommerceApiUrl"] ?? "http://localhost:5080";
        services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
        {
            client.BaseAddress = new Uri(ecommerceBaseUrl.EndsWith('/') ? ecommerceBaseUrl : $"{ecommerceBaseUrl}/");
        });

        // Autenticación JWT para proteger endpoints de PaymentService
        var jwtKey = configuration["Jwt:Key"] ?? "ECommerce-SuperSecret-Key-Min32Chars-2026!";
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "ECommerce.Api";
        var jwtAudience = configuration["Jwt:Audience"] ?? "ECommerce.Client";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();

        return services;
    }
}
