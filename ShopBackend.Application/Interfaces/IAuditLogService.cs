using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;

namespace ShopBackend.Application.Interfaces
{
    public interface IAuditLogService
    {

        Task<AuditLog> GetByIdAsync(int id);
        Task<AuditLog> CreateAsync(CreateAuditLogDto dto);
        Task<IEnumerable<AuditLog>> GetAllAsync();
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId); // Methode zum Abrufen aller Audit-Logs eines bestimmten Benutzers, z.B. um die Aktivitäten eines Benutzers zu überwachen oder zu analysieren.
        Task<IEnumerable<AuditLog>> GetByEntityIdAsync(int entityId); // Methode zum Abrufen aller Audit-Logs, die mit einer bestimmten Entität (z.B. einem Produkt oder einer Bestellung) verknüpft sind, um Änderungen an dieser Entität nachzuverfolgen.   
    }
}
