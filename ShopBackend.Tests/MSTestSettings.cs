using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using ShopBackend.Infrastructure.Services;

namespace ShopBackend.Tests.Services
{
    [TestClass]
    public class UserServiceTests
    {
        private AppDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        private UserService CreateService(AppDbContext context)
        {
            var configValues = new Dictionary<string, string>
            {
                {"Jwt:Key", "supersecretkey123456"},
                {"Jwt:Issuer", "test"},
                {"Jwt:Audience", "test"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            };

            return new UserService(context, configuration, httpContextAccessor);
        }

        [TestMethod]
        public async Task GetByIdAsync_UserExists_ReturnsUser()
        {
            using var context = GetDbContext("TestDb_Exists");

            var testUser = new User
            {
                Id = 1,
                Email = "test@test.de",
                PasswordHash = "hash",
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetByIdAsync(1);

            Assert.IsNotNull(result);
            Assert.AreEqual("test@test.de", result.Email);
        }

        [TestMethod]
        public async Task GetByIdAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            using var context = GetDbContext("TestDb_NotFound");

            var service = CreateService(context);

            try
            {
                await service.GetByIdAsync(99);
                Assert.Fail("Expected KeyNotFoundException was not thrown.");
            }
            catch (KeyNotFoundException ex)
            {
                Assert.IsTrue(ex.Message.Contains("99"));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Wrong exception type: {ex.GetType().Name}");
            }
        }
    }
}