using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/subprojects/import")]
[Authorize(Roles = Roles.PlanningStaff)]
public class ImportController : ControllerBase
{
    // Limit the file size to 10 MB
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly IImportService _importService;

    public ImportController(IImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("preview")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<ImportPreviewResultDto>> Preview(IFormFile file, [FromForm] int? financialYearId, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "برجاء اختيار ملف Excel" });
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "يجب أن يكون الملف بصيغة .xlsx" });
        }

        await using var stream = file.OpenReadStream();
        var result = await _importService.PreviewAsync(stream, financialYearId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("commit")]
    public async Task<ActionResult<ImportCommitResultDto>> Commit(ImportCommitDto dto, CancellationToken cancellationToken)
    {
        var result = await _importService.CommitAsync(dto, cancellationToken);
        return Ok(result);
    }
}
