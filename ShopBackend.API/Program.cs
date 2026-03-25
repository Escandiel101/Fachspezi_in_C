using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopBackend.API.Middleware;
using ShopBackend.Application.Authorization;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.BackgroundServices;
using ShopBackend.Infrastructure.Data;
using ShopBackend.Infrastructure.Services;
using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;


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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignoriert Endlosschleifen bei verknüpften Tabellen (Order -> Customer -> Order... Problem seit 24. März 2026 ongoing 10h Debuging)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

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
// builder.Services.AddOpenApi();

// Swagger Test Tool mit UI einfügen:
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,   // wichtig
        Scheme = "bearer",               
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// Alle builder. Parameter müssen vor dem hier stehen, sonst werden sie nicht "gebaut"
var app = builder.Build();

// legt einen Admin an, sonst wird das ganze etwas kniffelig, wenn nur ein Admin einen anderen Admin ernennen kann.
// Legt zusätzlich einen ganzen Seed mit Daten an, damit man direkt testen kann.
// Legt nur etwas an, wenn diese Daten nicht schon vorhanden sind --> (!Context.Entity.Any()):
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Admin User
    if (!context.Users.Any(u => u.Email == "admin@shop.de")) // man kann auch .Where(u =>....).Any(); schreiben.
    {
        context.Users.Add(new User
        {
            Email = "admin@shop.de",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    // Staff User
    if (!context.Users.Any(u => u.Email == "staff@shop.de"))
    {
        context.Users.Add(new User
        {
            Email = "staff@shop.de",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
            Role = UserRole.Staff,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    // Kunden (User + Customer Profil)
    if (!context.Users.Any(u => u.Email == "max.mustermann@mail.de")) // Kunde 1 zum freien Experimentieren.
    {
        var kunde1 = new User
        {
            Email = "max.mustermann@mail.de",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Kunde123!"),
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(kunde1);
        await context.SaveChangesAsync();

        context.Customers.Add(new Customer
        {
            UserId = kunde1.Id,
            FirstName = "Max",
            LastName = "Mustermann",
            Address = "Musterstraße 1, 12345 Musterstadt",
            Phone = "01234567890"
        });
        await context.SaveChangesAsync();
    }

    if (!context.Users.Any(u => u.Email == "anna.schmidt@mail.de"))  // Kunde 2 mit Bestellung und Rechnung zum Testen von Delete Exceptions.
    {
        var kunde2 = new User
        {
            Email = "anna.schmidt@mail.de",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Kunde123!"),
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(kunde2);
        await context.SaveChangesAsync();

        context.Customers.Add(new Customer
        {
            UserId = kunde2.Id,
            FirstName = "Anna",
            LastName = "Schmidt",
            Address = "Hauptstraße 42, 80331 München",
            Phone = "09876543210"
        });
        await context.SaveChangesAsync();
    }

    // Produkte (Erst Produkte anlegen, bevor man sie in ne Bestellung schreiben kann)
    if (!context.Products.Any())
    {
        var products = new List<Product>
        {
            new Product 
            { 
              Name = "Winterjacke", 
              Description = "Warme Winterjacke", 
              Price = 89.99m, TaxRate = 19, 
              IsActive = true 
            }, // zu viele Zeilen, besser so:
            new Product { Name = "Jeans", Description = "Klassische Jeans", Price = 49.99m, TaxRate = 19, IsActive = true },
            new Product { Name = "T-Shirt", Description = "Basic T-Shirt", Price = 19.99m, TaxRate = 19, IsActive = true },
            new Product { Name = "Hoodie", Description = "Gemütlicher Hoodie", Price = 39.99m, TaxRate = 19, IsActive = true },
            new Product { Name = "Sneaker", Description = "Sportliche Sneaker", Price = 69.99m, TaxRate = 19, IsActive = true },
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        foreach (var product in products)
        {
            context.Stocks.Add(new Stock
            {
                ProductId = product.Id,
                Quantity = 50,
                ReservedQuantity = 0
            });
        }
        await context.SaveChangesAsync();

        // Bestellung für Kunde 2 (Anna Schmidt) erzeugen:
        if (!context.Orders.Any())
        {
            // Erst Kunde 2 laden
            var anna = await context.Customers
                .Where(c => c.User.Email == "anna.schmidt@mail.de")
                .Include(c => c.User)
                .FirstOrDefaultAsync();

            var jeans = await context.Products
                .Where(p => p.Name == "Jeans")
                .FirstOrDefaultAsync();

            var hoodie = await context.Products
                .Where(p => p.Name == "Hoodie")
                .FirstOrDefaultAsync();

            // Bestellung ohne Rabattcode!
            if (anna != null && jeans != null && hoodie != null)
            {
                var order = new Order
                {
                    CustomerId = anna.Id,
                    Status = "ausstehend",
                    NetTotal = jeans.Price + hoodie.Price,
                    GrossTotal = (jeans.Price + hoodie.Price) * 1.19m,
                    OrderDate = DateTime.UtcNow
                };
                context.Orders.Add(order);
                await context.SaveChangesAsync();

                // Stock reservieren
                var jeansStock = await context.Stocks
                    .Where(s => s.ProductId == jeans.Id)
                    .FirstOrDefaultAsync();

                var hoodieStock = await context.Stocks
                    .Where(s => s.ProductId == hoodie.Id)
                    .FirstOrDefaultAsync();

                context.OrderItems.AddRange(new List<OrderItem>
            {
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = jeans.Id,
                    Quantity = 1,
                    UnitPrice = jeans.Price,
                    TaxRate = jeans.TaxRate
                },
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = hoodie.Id,
                    Quantity = 2,
                    UnitPrice = hoodie.Price,
                    TaxRate = hoodie.TaxRate
                }
            });

                if (jeansStock != null) jeansStock.ReservedQuantity += 1;
                if (hoodieStock != null) hoodieStock.ReservedQuantity += 2;

                await context.SaveChangesAsync();

                // Rechnung zur Bestellung
                context.Invoices.Add(new Invoice
                {
                    OrderId = order.Id,
                    NetTotal = order.NetTotal,
                    GrossTotal = order.GrossTotal,
                    TaxAmount = order.GrossTotal - order.NetTotal,
                    FirstName = anna.FirstName,
                    LastName = anna.LastName,
                    Address = anna.Address,
                    PaymentMethod = "Barzahlung",
                    Status = "Zahlung per Nachnahme"
                });

                // Order Status auf verarbeitet setzen
                order.Status = "verarbeitet";
                await context.SaveChangesAsync();
            }
        }
    }

    // Rabattcode (zum später einfügen oder für neue Bestellungen)
    if (!context.DiscountCodes.Any())
    {
        context.DiscountCodes.Add(new DiscountCode
        {
            Code = "Eröffnung20",
            DiscountPercentage = 20,
            MinOrderValue = 50,
            MaxUses = 200,
            UsedCount = 0,
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddMonths(6)
        });
        await context.SaveChangesAsync();
    }
}
// muss vor CORS und Routing stehen, damit HTTPS erzwungen wird. 
// theoretisch HTTP im Dev Modus zum Testen, Https im normalen Anwendungsfall.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// die Html Seiten verwenden als Basis im code für die API URL den localhost mit Port: 5139 für http im Dev-mode.
// Das Backend hier rettet es durch policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); - allerdings ist das recht unsicher.
// Außerdem merkt der Browser sich, wenn man 1x auf https war und versucht dann immer 7131 für sicheres https zu nehmen,
// Will man also explizit auf 5139,muss man den Port manuell in der Adresszeile eingeben.

// Warum dann http? Lädt lokal immer und mit nem ggf. anderen Setup gibts kein SSL Fehler Abbruch... spart Nerven nur zum testen.

// Fürs Wunsch Frontend braucht man statische Daten aktiviert:
// Sucht automatisch nach index.html im wwwroot Ordner.
app.UseDefaultFiles();
// Erlaubt dem Browser den Zugriff auf Admin.html, Shop.html etc.
app.UseStaticFiles(); 

// Muss vor der Swagger Pipeline stehen, sonst kann CORS (Cross-Origin Resource Sharing nicht kommunizieren und dann lehnt es mir im Swagger den Zugriff ab.
app.UseCors("AllowAll");

// HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    // Das hier generiert die Interface-Seite für Swagger
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shop API v1");
        // Prefix string.empty rausgenommen und durch swagger ersetzt, sonst kollidieren die htmls mit dem swagger.
        c.RoutePrefix = "swagger"; 
    }); 
}

app.UseMiddleware<ExceptionMiddleware>();

// Wer - liest Tokens und prüft die Signatur
app.UseAuthentication();

// Was - prüft z.B. die Rolle im Token
app.UseAuthorization();

app.MapControllers();

app.Run();
