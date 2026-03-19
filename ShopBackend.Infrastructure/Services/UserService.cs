using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
// kürzere Methode mit Alias, statt BCrypt.Net.BCrypt.Hashpassword(dto...) immer wieder schreiben zu müssen)
using BC = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Http;



namespace ShopBackend.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserService(AppDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }




        public async Task ChangePasswordAsync(int id, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");

           if (!BC.Verify(dto.CurrentPassword, user.PasswordHash)) // Es gibt keine alternaitve Formulierung, da BCrypt nicht deterministisch hasht, es kommt jedes mal n anderer Hash raus.
                throw new UnauthorizedAccessException("Falsches Passwort"); // Generische Antworten sind besser als Detailreiche. So weiß ein pot. Angreifer nicht was genau falsch ist. 

            user.PasswordHash = BC.HashPassword(dto.NewPassword); 
                await _context.SaveChangesAsync();
        }


        public async Task<User> CreateAsync(CreateUserDto dto)
        {
            var existingUser = await _context.Users
                .Where(u => u.Email == dto.Email)
                .AnyAsync();
            if (existingUser)
                throw new ArgumentException("Registrierung Fehlgeschlagen"); // das Frontend müsste dann hier ansetzen, das Backend gibt nur minimale Infos für potentielle Angreifer

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BC.HashPassword(dto.Password),
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;


            /* TypenUNsicherer, Fehleranfälliger... länger auch so möglich: 
            await _context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Users (Email, PasswordHash, Role, CreatedAt) VALUES ({0}, {1}, {2}, {3})",
            dto.Email, dto.Password, "Customer", DateTime.UtcNow);
            */
        }


        public async Task DeleteAsync(int id)
        {

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");

            // Man braucht hier keine else oder else if, da die Methode nach dem Throwen der Exception sowieso abgebrochen wird.
            // Es ist also nicht möglich, dass der Code weiterläuft, wenn der User nicht gefunden wurde.


            var openOrders = await _context.Orders
                .Where(o => o.Customer.UserId == id && o.Status != "storniert")
                .AnyAsync();
            if (openOrders)
                throw new ArgumentException("Es sind noch Bestellungen offen!");

            var openInvoices = await _context.Invoices
                .Where(i => i.Order.Customer.UserId == id && i.Status != "storniert" && i.Status != "bezahlt")
                .AnyAsync();
            if (openInvoices)
                throw new ArgumentException("Es sind noch Rechnungen offen!");

            _context.Users.Remove(user);

            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "User",      // Klassenname
                EntityId = user.Id,       // ID des geänderten Objekts
                Action = "Delete",         // Was wurde gemacht
                ChangedBy = changedBy,     // Wer hats gemacht
                Details = $"User mit der ID: {user.Id} ({user.Email}) gelöscht."           // Freitext
            });
            await _context.SaveChangesAsync();
        }


        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();

        }


        public async Task<User> GetByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) 
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");
            return user;
        }


        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {

            // User hier über die Email verifizieren statt über Id, da er User die ID noch nicht kennt.
            var user = await _context.Users
                .Where(u => u.Email == dto.Email)
                .FirstOrDefaultAsync();
            if (user == null)
                throw new UnauthorizedAccessException("Ungültige Eingabe.");

            // Prüfen, ob klartextPw dem gehashten Wert entspricht, wenn nein, Fehlerausgabe:
            if (!BC.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Ungültige Eingabe.");

            // Neue Hilfsfunktion zur Generierung des JSON Web Tokens:
            string token = GenerateJwtToken(user);

            // Erstellen der Response/Ausgabe für den user:
            var loginResponseDto = new LoginResponseDto
            { 
                Id = user.Id,
                Role = user.Role.ToString(),
                Token = token
            };

            return loginResponseDto;
        }


        public async Task UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");
            
            user.Email = dto.Email ?? user.Email; 
            await _context.SaveChangesAsync();
        }


        public async Task UpdateRoleAsync(int id, UpdateUserRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");

            user.Role = dto.Role;

            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "User",      
                EntityId = user.Id,       
                Action = "Delete",         
                ChangedBy = changedBy,     
                Details = $"Rolle von User {user.Id} ({user.Email}) auf {dto.Role} geändert."
            });
            await _context.SaveChangesAsync();
        }



        // Hilfsfunktionen:


        private string GenerateJwtToken(User user)
        {
            //Header.Payload.Signature für den JWT soll hier erstellt werden:

            // Werte aus den appsettings.Json holen:
            var jwtKey = _configuration["Jwt:Key"];             // Wird Teil der Signatur
            var issuer = _configuration["Jwt:Issuer"];          // Wird Teil des Payloads
            var audience = _configuration["Jwt:Audience"];      // Wird Teil des Payloads


            // Erstellen des Kryptografischen Vorgangs für die Signatur (Ist es echt?):
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));// ! hinten ist wie schon oft die compiler Beschwichtigung. Er warnt - ich kann nicht garantieren, dass der Wert nicht null sein wird, ich sage: ! passt so, ich stehe dafür ein, er ist nie null.
            // "creds" leitet bereits die Erstellung des Headers (durch den SecurityAlgo.hmac..) ein:
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Erstellen des Payloads, praktisch dem Body (Was ist drin?):
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())

            };

            // Setzt den Token als Ganzes zusammen:
            var token = new JwtSecurityToken
                (
                issuer: issuer,                                 //PL (Absender)
                audience: audience,                             //PL (Empfänger)
                claims: claims,                                 //PL (Daten)
                expires: DateTime.UtcNow.AddDays(1),            //PL (Ablaufdatum)
                signingCredentials: creds                       //Erzeugt den Header (Algo) & die Signatur

                );

            return new JwtSecurityTokenHandler().WriteToken(token);

        }

    }
}
