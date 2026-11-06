# PaymentService — Microservicio de Pagos Independiente

Microservicio de Pagos estructurado con **Clean Architecture (.NET 8)**, **CQRS**, **Entity Framework Core 8.0.11 con SQLite**, **Autenticación JWT** y **Swagger**.

---

## 🚀 Cómo levantar el microservicio

Desde esta carpeta (`PaymentService`):

```powershell
dotnet run --project Payment.Api/Payment.Api.csproj
```

- **Puerto HTTP:** `http://localhost:5081`
- **Swagger UI:** [http://localhost:5081/swagger](http://localhost:5081/swagger)

---

## 🏗️ Capas de la Solución (Clean Architecture)

- **`Payment.Domain`:** Entidad `PaymentTransaction` con encapsulación de reglas de negocio y enum `PaymentStatus`.
- **`Payment.Application`:** Commands (`ProcessPaymentCommand`), Queries (`GetPaymentByOrderIdQuery`), DTOs e Interfaces.
- **`Payment.Infrastructure`:** `PaymentDbContext` (SQLite `payment.db`), `PaymentRepository`, `MockPaymentProcessor` y configuración JWT.
- **`Payment.Api`:** Controlador `PaymentsController` protegido con `[Authorize]` y Swagger con soporte Bearer.
