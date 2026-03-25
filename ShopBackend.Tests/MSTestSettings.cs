//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using ShopBackend.Domain.Entities;
//using ShopBackend.Infrastructure.Data;
//using ShopBackend.Infrastructure.Services;
//using ShopBackend.Domain.Entities;

//namespace ShopBackend.Tests.Services
//{
//    [TestClass]
//    public class UserServiceTests
//    {
//        // Hilfsmethode, um den Context schnell zu erstellen 
//        private AppDbContext GetDbContext(string dbName)
//        {
//            var options = new DbContextOptionsBuilder<AppDbContext>()
//                .UseInMemoryDatabase(databaseName: dbName)
//                .Options;
//            return new AppDbContext(options);
//        }

//        [TestMethod]
//        public async Task GetByIdAsync_UserExists_ReturnsUser()
//        {
//            // Arrange
//            using var context = GetDbContext("TestDb_Exists");
//            var testUser = new User
//            {
//                Id = 1,
//                Email = "test@test.de",
//                PasswordHash = "hash",
//                Role = UserRole.Customer,
//                CreatedAt = DateTime.UtcNow
//            };
//            context.Users.Add(testUser);
//            await context.SaveChangesAsync();

//            var configuration = new ConfigurationBuilder().Build();
//            var httpContextAccessor = new HttpContextAccessor();
//            var service = new UserService(context, configuration, httpContextAccessor);
//            var configValues = new Dictionary<string, string>
//            {
//                {"Jwt:Key", "supersecretkey123456"},
//                {"Jwt:Issuer", "test"},
//                {"Jwt:Audience", "test"}
//            };

//            var configuration = new ConfigurationBuilder()
//                .AddInMemoryCollection(configValues)
//                .Build();

//            var httpContextAccessor = new HttpContextAccessor
//            {
//                HttpContext = new DefaultHttpContext()
//            };

//            var service = new UserService(context, configuration, httpContextAccessor);

//            // Act
//            var result = await service.GetByIdAsync(1);

//            // Assert
//            Assert.IsNotNull(result);
//            Assert.AreEqual("test@test.de", result.Email);
//        }

//        [TestMethod]
//        public async Task GetByIdAsync_UserNotFound_ThrowsKeyNotFoundException()
//        {
//            // Arrange
//            using var context = GetDbContext("TestDb_NotFound");
//            var configuration = new ConfigurationBuilder().Build();
//            var httpContextAccessor = new HttpContextAccessor();
//            var service = new UserService(context, configuration, httpContextAccessor);
//            var configValues = new Dictionary<string, string>
//            {
//                {"Jwt:Key", "supersecretkey123456"},
//                {"Jwt:Issuer", "test"},
//                {"Jwt:Audience", "test"}
//            };

//            var configuration = new ConfigurationBuilder()
//                .AddInMemoryCollection(configValues)
//                .Build();

//            var httpContextAccessor = new HttpContextAccessor
//            {
//                HttpContext = new DefaultHttpContext()
//            };

//            var service = new UserService(context, configuration, httpContextAccessor);





//            // Act & Assert


//            try
//            {
//                await service.GetByIdAsync(99);

//                // Wenn wir hier ankommen, hat der Service KEINE Exception geworfen
//                Assert.Fail("Die erwartete KeyNotFoundException wurde nicht geworfen.");
//            }
//            catch (KeyNotFoundException ex)
//            {
//                // Assert: Wir prüfen, ob die Fehlermeldung stimmt
//                // (Optional, aber gut für die Punkte in der Prüfung!)
//                Assert.IsTrue(ex.Message.Contains("99"), "Fehlermeldung sollte die ID enthalten.");
//            }
//            catch (Exception ex)
//            {
//                // Falls eine GANZ andere Exception kommt, soll der Test auch fehlschlagen
//                Assert.Fail($"Falscher Exception-Typ geworfen: {ex.GetType().Name}");
//            }
//        }
//    }
//}



