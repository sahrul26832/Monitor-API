using DemoResendInterface.Models.DTOs;
using DemoResendInterface.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DemoResendInterface.Controllers;

/// <summary>
/// Controller: Request Routes
/// Thin controller — delegates to IRequestService, no business logic.
/// </summary>
[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _service;

    public RequestsController(IRequestService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/requests — list all requests with optional filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? method,
        [FromQuery] string? status,
        [FromQuery] string? url,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo)
    {
        var data = await _service.GetAllAsync(method, status, url, dateFrom, dateTo);
        return Ok(data);
    }

    /// <summary>
    /// GET /api/requests/{id} — get single request
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data is null)
            return NotFound(new { error = "Request not found" });
        return Ok(data);
    }

    /// <summary>
    /// POST /api/requests — create new request log
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return StatusCode(201, result);
    }

    /// <summary>
    /// POST /api/requests/{id}/resend — resend a failed request
    /// </summary>
    [HttpPost("{id:guid}/resend")]
    public async Task<IActionResult> Resend(Guid id)
    {
        var result = await _service.ResendAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// PATCH /api/requests/{id}/ignore — mark request as IGNORED
    /// </summary>
    [HttpPatch("{id:guid}/ignore")]
    public async Task<IActionResult> Ignore(Guid id)
    {
        var result = await _service.IgnoreAsync(id);
        return Ok(result);
    }
}
