using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Services;

public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public OrderServiceClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<OrderDetailsDto?> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/orders/{orderId}");

            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
            {
                request.Headers.Authorization = headerValue;
            }
            else
            {
                var serviceToken = GenerateInternalServiceToken();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
            }

            using var response = await _httpClient.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.IsSuccessStatusCode)
            {
                var dto = await response.Content.ReadFromJsonAsync<OrderDetailsDto>(cancellationToken: ct);
                return dto is null ? null : dto with { Status = TranslateStatusToSpanish(dto.Status) };
            }
        }
        catch (HttpRequestException)
        {
            // Si ECommerce.Api no está corriendo por HTTP en ese instante, intentamos leer de la base de datos local de ECommerce como respaldo
        }

        return await TryGetOrderFromLocalDatabaseAsync(orderId, ct);
    }

    private static string TranslateStatusToSpanish(string? rawStatus) => (rawStatus?.Trim() ?? "Pendiente") switch
    {
        "0" or "Pending" or "Pendiente" => "Pendiente",
        "1" or "Confirmed" or "Approved" or "Aprobado" => "Aprobado",
        "4" or "Cancelled" or "Rejected" or "Rechazado" => "Rechazado",
        var other => other
    };

    public async Task ConfirmOrderInECommerceAsync(int orderId, CancellationToken ct = default)
    {
        var dbPath = FindECommerceDbPath();
        if (dbPath is null)
        {
            return;
        }

        await using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync(ct);

        string? columnType = null;
        await using (var typeCmd = connection.CreateCommand())
        {
            typeCmd.CommandText = "SELECT typeof(Status) FROM Orders WHERE Id = $id LIMIT 1";
            typeCmd.Parameters.AddWithValue("$id", orderId);
            var result = await typeCmd.ExecuteScalarAsync(ct);
            columnType = result?.ToString();
        }

        if (columnType is null)
        {
            return;
        }

        await using var updateCmd = connection.CreateCommand();
        if (string.Equals(columnType, "text", StringComparison.OrdinalIgnoreCase))
        {
            updateCmd.CommandText = "UPDATE Orders SET Status = 'Confirmed' WHERE Id = $id";
        }
        else
        {
            // En ECommerce.Domain.Entities.OrderStatus: 0 = Pending, 1 = Confirmed
            updateCmd.CommandText = "UPDATE Orders SET Status = 1 WHERE Id = $id";
        }

        updateCmd.Parameters.AddWithValue("$id", orderId);
        await updateCmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<OrderDetailsDto?> TryGetOrderFromLocalDatabaseAsync(int orderId, CancellationToken ct)
    {
        var dbPath = FindECommerceDbPath();
        if (dbPath is null)
        {
            throw new HttpRequestException("No se pudo conectar con ECommerce.Api en http://localhost:5080 y no se encontró ecommerce.db.");
        }

        await using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        await connection.OpenAsync(ct);

        int id;
        int userId;
        string statusStr;
        decimal total;
        DateTime createdAt;

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, UserId, Status, Total, CreatedAt FROM Orders WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", orderId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                return null;
            }

            id = reader.GetInt32(0);
            userId = reader.GetInt32(1);

            var rawStatus = reader.GetValue(2)?.ToString();
            statusStr = TranslateStatusToSpanish(rawStatus);

            total = Convert.ToDecimal(reader.GetValue(3), CultureInfo.InvariantCulture);
            createdAt = DateTime.TryParse(reader.GetValue(4)?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt)
                ? dt
                : DateTime.UtcNow;
        }

        var items = new List<OrderItemDetailsDto>();
        await using (var itemCmd = connection.CreateCommand())
        {
            itemCmd.CommandText = "SELECT ProductId, UnitPrice, Quantity FROM OrderItems WHERE OrderId = $orderId";
            itemCmd.Parameters.AddWithValue("$orderId", orderId);

            await using var itemReader = await itemCmd.ExecuteReaderAsync(ct);
            while (await itemReader.ReadAsync(ct))
            {
                var productId = itemReader.GetInt32(0);
                var unitPrice = Convert.ToDecimal(itemReader.GetValue(1), CultureInfo.InvariantCulture);
                var quantity = itemReader.GetInt32(2);
                items.Add(new OrderItemDetailsDto(productId, unitPrice, quantity, unitPrice * quantity));
            }
        }

        return new OrderDetailsDto(id, userId, statusStr, total, createdAt, items);
    }

    private static string? FindECommerceDbPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "PROYECTO--BACKEND-main", "eccomerce", "ECommerce.Api", "ecommerce.db");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private string GenerateInternalServiceToken()
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "ECommerce-SuperSecret-Key-Min32Chars-2026!";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "ECommerce.Api";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "ECommerce.Client";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("service", "PaymentService")
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
