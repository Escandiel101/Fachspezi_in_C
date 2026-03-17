using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.Interfaces;
using ShopBackend.Infrastructure.Data;
using ShopBackend.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


//    Dependency Injection (DI) Container 
// Registriert die Business-Logik-Services, damit sie per Constructor Injection in den Controllern (oder anderen Services) verfügbar sind.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDiscountCodeService, DiscountCodeService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductStockService, ProductStockService>();

// Notwendig für Dependecy Injection, um die Services über die Http Methoden der Controller zu injezieren. 
builder.Services.AddControllers();

//    Authentifizierung & JWT 
// Konfiguriert den Schutzwall der API. Legt fest, dass JWT genutzt werden soll und wie ein gültiges "Ticket" (Token) aussehen muss.
// Definiert, wie das Backend eingehende Token auf Echtheit prüft (Signatur, Issuer, Ablaufdatum)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,              // Kommt der Token überhaupt von meinem Server?
            ValidateAudience = true,            // Ist er für meine App gedacht?
            ValidateLifetime = true,            // Ist er zeitlich überhaupt noch gültig?
            ValidateIssuerSigningKey = true,    // Ist die digitale Unterschrift echt?

            ValidIssuer = builder.Configuration["Jwt:Issuer"],                                                          // Wäre hier das ShopFrontend
            ValidAudience = builder.Configuration["Jwt:Audience"],                                                      // das ShopBackend
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))      // Der Key ist sozusagen das Heiligtum meines Backend´s. Er liegt in der appsettings.Json und
            //fungiert als digitale Signatur, die hier gehasht wird. Sie wird dann z.B. dem user mitgegeben, wenn er sich einloggt und wenn der etwas ändern will, wird die Signatur wieder mitgeschickt. 
            // Stimmen die Hash-Werte überein, so kann der User etc. gemäß seiner Rolle Dinge ändern. JWT --> Header.Payload.Signatur.
        };

    });

// Service für IAuthorization in der Application mit den Klassen Requirement und Handler für Sicherheits Policy der einzelnen Rollen.
builder.Services.AddHttpContextAccessor();






// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Alle builder. Parameter müssen vor dem hier stehen, sonst werden sie nicht "gebaut"
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Wer - liest Tokens und prüft die Signatur
app.UseAuthentication();

// Was - prüft z.B. die Rolle im Token
app.UseAuthorization();

app.MapControllers();

app.Run();
