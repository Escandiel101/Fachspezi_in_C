using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<AuditLog> CreateAsync(CreateAuditLogDto dto)
        {
            var auditLog = new AuditLog
            {
                EntityName = dto.EntityName,
                EntityId = dto.EntityId,
                Action = dto.Action,
                Details = dto.Details,
                ChangedBy = dto.ChangedBy,
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
