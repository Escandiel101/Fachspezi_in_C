using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ShopBackend.Infrastructure.Services
{
    // Wichtig: AuditLogService.cs ist die Klasse - Der Bauplan
    // Sobald ASP.NET Core bei einem Request den Konstruktor aufruft und den Service erstellt, ist das Ergebnis keine Klasse mehr, sondern ein Objekt bzw. eine Instanz dieses Bauplans.
    public class AuditLogService : IAuditLogService  // Implementiert die Aufgaben, die im Interface stehen. Der Service implementiert das Interface.
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        // Der Service ist das Arbeitstier, welcher die Aufgaben (Methoden) vom Interface mit funktionsfähigem Code füllt.
        private readonly AppDbContext _context; // nur dieser Service hier darf den AppDbContext sehen.
        public AuditLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor) // Der Konstruktor initialisiert das Objekt bzw. die Instanz AuditLogService mit den Werkzeugen, die ASP.NET dem Konstruktor
                                                                                               // aus der bereits existierenden Instanz AppDbContext heraus in den Konstruktor hinein "reicht". Es wird dann in _context = context gespeichert.
                                                                                               // So kann dieses Werkzeug in allen methoden des Services benutzt werden.
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }


        

        public async Task<AuditLog> CreateAsync(CreateAuditLogDto dto)
        {
            // Auditlog tracken macht wenig Sinn, zumal das Create wohl in nem Dauerloop enden würde, aber das Szenario eines Bösen Admins muss ich trotzdem abfangen.
            // Jetzt kann niemand mehr einfach so Einträge für andere Teammitglieder erstellen, um z.B. eine Suspicious Activity zu verschleiern.
            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            var auditLog = new AuditLog
            {
                EntityName = dto.EntityName,
                EntityId = dto.EntityId,
                Action = dto.Action,
                Details = dto.Details,
                ChangedBy = changedBy
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }


        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            return await _context.AuditLogs.ToListAsync();
        }


        public async Task<IEnumerable<AuditLog>> GetByChangedByAsync(string changedBy)
        {
            var auditLogs = await _context.AuditLogs
                .Where(a => a.ChangedBy == changedBy)
                .ToListAsync();
            // If Prüfung, wenn kein Logfile vom Mitarbeiter X existiert, gibts ne leere Liste. Da dies ohnehin nur Admins bearbeiten, die den Code kennen, ist eine Fehlermeldung überflüssig imho.
            return auditLogs;
        }


        public async Task<IEnumerable<AuditLog>> GetByEntityIdAsync(int entityId)
        {
            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityId == entityId)
                .ToListAsync();

            // Genau wie bei GetByChangedBy: Wenn nichts gefunden wird, ist die Liste einfach leer.
            return auditLogs;
        }


        public async Task<AuditLog> GetByIdAsync(int id)
        {
            var auditLog = await _context.AuditLogs.FindAsync(id);
            if (auditLog == null)
                throw new KeyNotFoundException($"Auditlogfile mit der ID: {id} nicht gefunden.");
            
            return auditLog;
            
        }
    }
}
