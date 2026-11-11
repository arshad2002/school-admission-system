using AdmissionSystem.Api.DTOs;
using AdmissionSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdmissionSystem.Api.Controllers;

[ApiController]
public class AdmissionController : ControllerBase
{
    private readonly AdmissionService _service;

    public AdmissionController(AdmissionService service)
    {
        _service = service;
    }

    // ── Helper ──────────────────────────────────────────────

    private string? GetStudentId()
    {
        if (!Request.Headers.TryGetValue("X-Student-Id", out var values))
            return null;

        var studentId = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(studentId) || studentId.Length != 17 || !studentId.All(char.IsDigit))
            return null;

        return studentId;
    }

    // ── Class Endpoints ─────────────────────────────────────

    /// <summary>Create a new class.</summary>
    [HttpPost("classes")]
    [ProducesResponseType(typeof(CreateClassResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.TotalSeats <= 0)
            return BadRequest(new { error = "Invalid class name or totalSeats." });

        var result = await _service.CreateClassAsync(request);
        return Created($"/classes/{result.Id}", result);
    }

    /// <summary>List all classes with seat availability.</summary>
    [HttpGet("classes")]
    [ProducesResponseType(typeof(List<ClassDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListClasses()
    {
        var classes = await _service.ListClassesAsync();
        return Ok(classes);
    }

    /// <summary>Get details for a specific class.</summary>
    [HttpGet("classes/{classId}")]
    [ProducesResponseType(typeof(ClassDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClass(string classId)
    {
        var result = await _service.GetClassAsync(classId);
        if (result is null)
            return NotFound(new { error = "Class not found" });

        return Ok(result);
    }

    // ── Hold Endpoints ──────────────────────────────────────

    /// <summary>Apply for a seat hold in a class.</summary>
    [HttpPost("classes/{classId}/holds")]
    [ProducesResponseType(typeof(HoldResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateHold(string classId)
    {
        var studentId = GetStudentId();
        if (studentId is null)
            return BadRequest(new { error = "Missing or invalid X-Student-Id header. Must be a 17-digit number." });

        var (hold, error) = await _service.CreateHoldAsync(classId, studentId);
        if (hold is null)
        {
            if (error == "Class not found")
                return NotFound(new { error });

            return Conflict(new { error });
        }

        return Created($"/holds/{hold.HoldId}", hold);
    }

    /// <summary>Check the status of a hold.</summary>
    [HttpGet("holds/{holdId}")]
    [ProducesResponseType(typeof(HoldStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHold(string holdId)
    {
        var result = await _service.GetHoldAsync(holdId);
        if (result is null)
            return NotFound(new { error = "Hold not found" });

        return Ok(result);
    }

    /// <summary>Confirm a hold (pay fee).</summary>
    [HttpPost("holds/{holdId}/confirm")]
    [ProducesResponseType(typeof(ConfirmHoldResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmHold(string holdId)
    {
        var studentId = GetStudentId();
        if (studentId is null)
            return BadRequest(new { error = "Missing or invalid X-Student-Id header. Must be a 17-digit number." });

        var (response, statusCode, error) = await _service.ConfirmHoldAsync(holdId, studentId);

        return statusCode switch
        {
            200 => Ok(response),
            403 => StatusCode(403, new { error }),
            404 => NotFound(new { error }),
            409 => Conflict(new { error }),
            _ => StatusCode(statusCode, new { error })
        };
    }

    /// <summary>Cancel a hold.</summary>
    [HttpDelete("holds/{holdId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelHold(string holdId)
    {
        var studentId = GetStudentId();
        if (studentId is null)
            return BadRequest(new { error = "Missing or invalid X-Student-Id header. Must be a 17-digit number." });

        var (statusCode, error) = await _service.CancelHoldAsync(holdId, studentId);

        return statusCode switch
        {
            204 => NoContent(),
            403 => StatusCode(403, new { error }),
            404 => NotFound(new { error }),
            _ => StatusCode(statusCode, new { error })
        };
    }

    // ── Student Endpoints ───────────────────────────────────

    /// <summary>List all holds for the authenticated student.</summary>
    [HttpGet("me/holds")]
    [ProducesResponseType(typeof(List<MyHoldResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyHolds()
    {
        var studentId = GetStudentId();
        if (studentId is null)
            return BadRequest(new { error = "Missing or invalid X-Student-Id header. Must be a 17-digit number." });

        var holds = await _service.GetStudentHoldsAsync(studentId);
        return Ok(holds);
    }
}
