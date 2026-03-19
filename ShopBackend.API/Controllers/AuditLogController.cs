using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using System.Reflection;



namespace ShopBackend.API.Controllers
{
    [ApiController]

    //das [controller] wird bei der Ausgabe der URL automatisch durch den Klassennamen ersetzt, also hier praktisch api/auditLog/5 ( z.B. 5 für Id = 5 bei GetById). 
    [Route("api/[controller]")]
    [Authorize (Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        // private readonly friert den Zugriff auf das Feld ein, es kann nur im Konstruktor gesetzt werden, danach ist es read-only.
        // Das ist best practice, um sicherzustellen, dass die Abhängigkeit nicht versehentlich geändert wird.
        private readonly IAuditLogService _auditLogService;

               /* Request kommt rein -> api/auditLog/...
                    * ASP.NET Core: "Ich brauche einen OrderController"
                    * Schaut in den Konstruktor: "Der braucht einen IAuditLogService"
                    * Holt den IAuditLogService aus dem DI-Container(der im Program.cs mit den builder.Services registriert wurde.)
                    * Erstellt den Controller mit dem Service
                    * Führt den Endpoint (die jeweilige Methode z.B. HttpGet) aus
                    * Controller wird am Ende jeweils wieder weggeworfen ->  Die Controller existieren nur einen Moment, bis deren Aufgabe/Methode erfüllt wurde.
               */
        public AuditLogController(IAuditLogService auditLogService) // <- Das ist der Konstruktor; ohne den wäre eine Dependency Injection für ASP.Net Core nicht möglich.
        {
            // das _ hier hat keine reale Auswirkung, es ist nur Konvention, um private Felder von lokalen Variablen zu unterscheiden.
            // Es könnte auch einfach auditLogService oder banana heißen, aber das _ macht es übersichtlicher.
            _auditLogService = auditLogService;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var auditLogs = await _auditLogService.GetAllAsync();
            return Ok(auditLogs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var auditLog = await _auditLogService.GetByIdAsync(id);
            return Ok(auditLog);
        }

        [HttpGet("changedBy/{changedBy}")]
        public async Task<IActionResult> GetByChangedBy(string changedBy)
        {
            var auditLog = await _auditLogService.GetByChangedByAsync(changedBy);
            return Ok(auditLog);
        }

        [HttpGet("entityId/{entityId}")]
        public async Task<IActionResult> GetByEntityId(int entityId)
        {
            var auditLog = await _auditLogService.GetByEntityIdAsync(entityId);
            return Ok(auditLog);    
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAuditLogDto dto)
        {
            var auditLog = await _auditLogService.CreateAsync(dto);
            // wieder ...(nameof(Endpoint Name), new {Routen Parameter}, Response Body)
            return CreatedAtAction(nameof(GetById), new { id = auditLog.Id }, auditLog);
        }

    }
}
