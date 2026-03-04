using System;
using Microsoft.EntityFrameworkCore;
using ShopBackend.Domain.Entities;

namespace ShopBackend.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {  
          // Sicherstellen, dass beim Löschen eines Kunden nicht seine Bestellungen verschwinden
          modelBuilder.Entity<Order>().HasOne(o => o.Customer).WithMany(c => c.Orders).OnDelete(DeleteBehavior.Restrict);
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

    }


}   
