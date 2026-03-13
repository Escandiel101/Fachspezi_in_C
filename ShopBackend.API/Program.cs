using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.Interfaces;
using ShopBackend.Infrastructure.Data;
using ShopBackend.Infrastructure.Services;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Builder Services der Services zu den Interfaces im Program.cs hinzufügen:
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductStockService, ProductStockService>();



// Ohne das hier kann Dependecy Injection die Services nicht in die http Controller injezieren. 
builder.Services.AddControllers();








// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
