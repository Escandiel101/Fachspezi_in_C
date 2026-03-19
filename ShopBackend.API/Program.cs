using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopBackend.API.Middleware;
using ShopBackend.Application.Authorization;
using ShopBackend.Application.Interfaces;
using ShopBackend.Infrastructure.BackgroundServices;
using ShopBackend.Infrastructure.Data;
using ShopBackend.Infrastructure.Services;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Transient erstellt jedes Mal ein neues Objekt, wenn im Code nach einem Service gefragt wird, gut für leichte Services die keinen Zustand speichern müssen.
// Scoped erstellt einmal pro HTTP-Request, alle Klassen die während dieses einen "klicks=Request" den Service aufrufen bekommen dasselbe Objekt. Es wird beim Start der Anfrage erstellt und nach dem Senden der Antwort (Response) sofort zerstört
// Singelton erstellt das Objekt ein einziges Mal zum Start der App, danach benutzen ALLE User und ALLE Anfragen dasselbe Objekt, bis der Server offline geht. (gut für Config, Caching, Accessor)


//    Dependency Injection (DI) Container 
// Registriert die Business-Logik-Services, damit sie per Constructor Injection in den Controllern (oder anderen Services) verfügbar sind.
// Scoped hier da, das Objekt für die Dauer eines Requests(klicks) existieren muss. Ideal für den DbContext, da eine Datenbankverbindung für den einen User-Klick geteilt wird.
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDiscountCodeService, DiscountCodeService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductStockService, ProductStockService>();

// Notwendig für Dependecy Injection, um die Services über die Http Methoden der Controller zu injezieren. 
builder.Services.AddControllers();

// Service der Im Hintergrund mitläuft und User, die sich längere Zeit nicht einloggen aus dem System nimmt (= Inactive Role)
builder.Services.AddHostedService<IdleUserDeactivator>(); 

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

/*            ! Der  HttpContextAccessor bekommt automatisch das Singelton zugewiesen
 
    Der ResourceHandler würde normalerweise sinnvoll Transient bekommen, ABER mein Code erstellt einen JWT Token für UtcNow.AddDay(1).
    Infolge dessen könnte ein vor einer Stunde ausgeschiedener Mitarbeiter noch 23 Stunden auf das System zugreifen.
    Deswegen muss der Handler hier mit der Datenbank sprechen können, um jederzeit prüfen zu können "Hey darf der das noch, oder ist der Mitarbeiter Inaktiv?".
    (Ein alternativer Token-Refresh auf 15min etc. mag sinnvoll erscheinen, aber ein bitchy Senior-Dev-Admin kann in 15min die Hölle auf Erden wahr werden lassen, denke ich)
 
    --> Der Handler bekommt Scoped 
*/
builder.Services.AddScoped<IAuthorizationHandler, IsResourceOwnerHandler>();
builder.Services.AddAuthorization(options =>        // Policy Optionen für die Anwendung auf das Interne Backend, um Zugriffsrechte zu steuern
{
    options.AddPolicy("IsResourceOwner", policy =>        // Hier steht nur OwnerOnly, aber die Zugriffsberechtigungen werden im ResourceHandler geHandeled. 
    {
        policy.Requirements.Add(new IsResourceOwnerRequirement());   // das Requirement ist die Vorraussetzung, der Auftrag wenn man so will, während der Handler entscheidet: "Wer darf durch?"

    });
});


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

app.UseMiddleware<ExceptionMiddleware>();

// Wer - liest Tokens und prüft die Signatur
app.UseAuthentication();

// Was - prüft z.B. die Rolle im Token
app.UseAuthorization();

app.MapControllers();

app.Run();
