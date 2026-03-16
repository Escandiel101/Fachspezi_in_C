using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.Interfaces;
using ShopBackend.Application.DTOs;



namespace ShopBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
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
            // wieder nameof((Endpoint Name), new {Routen Parameter}, Response Body)
            return CreatedAtAction(nameof(GetById), new { id = auditLog.Id }, auditLog);
        }

    }
}
