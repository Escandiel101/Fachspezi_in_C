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

            // Neu Passwortrichtlinien: 
            ValidatePassword(dto.NewPassword);

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

            // Neu Passwortrichtlinien: 
            ValidatePassword(dto.Password);

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BC.HashPassword(dto.Password),
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // save in die DB
            // Setzten des Hashes im Arbeitsspeicher für API Sicherheit: 
            // ohne Response DTOs kann ich das hier an dieser Stelle nicht anders lösen, um die API Zugriffe abzufangen.
            user.PasswordHash = "";
            _context.Entry(user).State = EntityState.Detached;
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

            // Neu: Mein Superadmin kann nicht Selbstmord begehen oder gelöscht werden :)
            if (id == 1)
            {
                throw new ArgumentException("Der System-Administrator (ID 1) kann nicht gelöscht werden.");
            }



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
            // Neu um den PW Hash nicht mitanzuzeigen.
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            { 
                user.PasswordHash = "";
                _context.Entry(user).State = EntityState.Detached;
            }
            return users;
        }


        public async Task<User> GetByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) 
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");

            user.PasswordHash = "";
            _context.Entry(user).State = EntityState.Detached;
            return user;
        }


        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {

            // User hier über die Email verifizieren statt über Id, da er User die ID noch nicht kennt.
            var user = await _context.Users
                .Where(u => u.Email == dto.Email)
                .Include(u => u.Customer)
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
                Token = token,
                CustomerId = user.Customer?.Id // Neu, da sonst mein Frontend keinen gültigen Login in den Shop bekommt, ohne die CustomerId zu kennen
            };

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Irgendwie wurde der user.PasswordHash =""; in die DB geschrieben, wie genau ist allerdings unklar. Die KI hat zwar Angebote, aber die sind alle unlogisch.
            // Daher das drunter entityState.detached (= zeig nur den hash nicht an, dann vergiss es direkt wieder aus dem Arbeitsspeicher, kein Speichern möglich)
            user.PasswordHash = "";
            _context.Entry(user).State = EntityState.Detached;
            // Schwuppdiwupp hatte mein Admin keinen korrekten Salt mehr im Hash :D

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

            // Neu: Schutz des SystemAdmins vor Rollenänderung wie im Delete.

            if (id == 1)
            {
                throw new ArgumentException("Die Rolle des System-Administrators (ID 1) darf nicht geändert werden.");
            }

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

            /* passt alles: // Debuging:
            Console.WriteLine("JWT KEY: " + jwtKey);        
            Console.WriteLine("ISSUER: " + issuer);         
            Console.WriteLine("AUDIENCE: " + audience);
            */

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
            // var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            // Console.WriteLine("TOKEN: " + tokenString);  // debuging Versuch 
            return new JwtSecurityTokenHandler().WriteToken(token);
            
        }

        // Passwort Richtlinien:
        private void ValidatePassword(string password)
        {
            if (password.Length < 8)
                throw new ArgumentException("Passwort muss mindestens 8 Zeichen lang sein.");
            if (!password.Any(char.IsUpper))
                throw new ArgumentException("Passwort muss mindestens einen Großbuchstaben enthalten.");
            if (!password.Any(char.IsDigit))
                throw new ArgumentException("Passwort muss mindestens eine Zahl enthalten.");
            if (!password.Any(char.IsSymbol) && !password.Any(char.IsPunctuation))
                throw new ArgumentException("Passwort muss mindestens ein Sonderzeichen enthalten.");
            if (password.Any(char.IsWhiteSpace))
                throw new ArgumentException("Passwort darf keine Leerzeichen enthalten.");
        }


    }
}
