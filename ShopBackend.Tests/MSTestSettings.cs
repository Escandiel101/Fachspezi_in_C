using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using ShopBackend.Infrastructure.Data;
using ShopBackend.Infrastructure.Services;
using ShopBackend.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShopBackend.Tests.Services
{
    [TestClass]
    public class UserServiceTests
    {
        // Hilfsmethode, um den Context schnell zu erstellen 
        private AppDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        [TestMethod]
        public async Task GetByIdAsync_UserExists_ReturnsUser()
        {
            // Arrange
            using var context = GetDbContext("TestDb_Exists");
            var testUser = new User
            {
                Id = 1,
                Email = "test@test.de",
                PasswordHash = "hash",
                Role = "Customer",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var service = new UserService(context);

            // Act
            var result = await service.GetByIdAsync(1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test@test.de", result.Email);
        }

        [TestMethod]
        public async Task GetByIdAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = GetDbContext("TestDb_NotFound");
            var service = new UserService(context);





            // Act & Assert
            
            // Fully AI Made... je 15mins ChatGpt, Claude + Sonnet, Deepseek fail, nur Gemini Ultra hats geschafft... :
            try
            {
                await service.GetByIdAsync(99);

                // Wenn wir hier ankommen, hat der Service KEINE Exception geworfen
                Assert.Fail("Die erwartete KeyNotFoundException wurde nicht geworfen.");
            }
            catch (KeyNotFoundException ex)
            {
                // Assert: Wir prüfen, ob die Fehlermeldung stimmt
                // (Optional, aber gut für die Punkte in der Prüfung!)
                Assert.IsTrue(ex.Message.Contains("99"), "Fehlermeldung sollte die ID enthalten.");
            }
            catch (Exception ex)
            {
                // Falls eine GANZ andere Exception kommt, soll der Test auch fehlschlagen
                Assert.Fail($"Falscher Exception-Typ geworfen: {ex.GetType().Name}");
            }
        }
    }
}



