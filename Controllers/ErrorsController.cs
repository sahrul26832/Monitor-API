using DemoResendInterface.Models.DTOs;
using DemoResendInterface.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DemoResendInterface.Controllers;

/// <summary>
/// Controller: Error Routes
/// Thin controller — delegates to IErrorService, no business logic.
/// </summary>
[ApiController]
[Route("api/errors")]
public class ErrorsController : ControllerBase
{
    private readonly IErrorService _service;

    public ErrorsController(IErrorService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/errors — list all errors with optional filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? errorCode,
        [FromQuery] string? resolved)
    {
        var data = await _service.GetAllAsync(category, errorCode, resolved);
        return Ok(data);
    }

    /// <summary>
    /// POST /api/errors — create new error log
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateErrorDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return StatusCode(201, result);
    }

    /// <summary>
    /// PATCH /api/errors/{id}/resolve — mark error as resolved
    /// </summary>
    [HttpPatch("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var result = await _service.ResolveAsync(id);
        return Ok(result);
    }
}
