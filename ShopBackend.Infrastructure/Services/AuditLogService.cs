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
            // If Prüfung, wenn kein Logfile vom Mitarbeiter X existiert, halte ich für überflüssig. Die Liste ist dann halt leer. 
            return auditLogs;
        }


        public Task<IEnumerable<AuditLog>> GetByEntityIdAsync(int entityId)
        {
            throw new NotImplementedException();
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
