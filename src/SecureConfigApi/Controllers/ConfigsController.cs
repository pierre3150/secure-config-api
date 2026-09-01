using Microsoft.AspNetCore.Mvc;
using SecureConfigApi.Models;
using SecureConfigApi.Services;

namespace SecureConfigApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigsController : ControllerBase
{
    private readonly IConfigService _configService;

    public ConfigsController(IConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>List all config keys (metadata only, values stay encrypted/hidden).</summary>
    [HttpGet]
    public async Task<ActionResult<List<ConfigEntryResponse>>> GetAll([FromQuery] string? environment)
    {
        return Ok(await _configService.GetAllAsync(environment));
    }

    /// <summary>Get the decrypted value for a specific key + environment.</summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<string>> GetValue(string key, [FromQuery] string environment = "production")
    {
        var value = await _configService.GetDecryptedValueAsync(key, environment);
        return value is null ? NotFound() : Ok(new { key, environment, value });
    }

    /// <summary>Create or update a config entry (value is encrypted before storage).</summary>
    [HttpPost]
    public async Task<ActionResult<ConfigEntryResponse>> Upsert([FromBody] ConfigEntryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key) || string.IsNullOrWhiteSpace(dto.Value))
            return BadRequest("Key and Value are required.");

        var result = await _configService.UpsertAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key, [FromQuery] string environment = "production")
    {
        var deleted = await _configService.DeleteAsync(key, environment);
        return deleted ? NoContent() : NotFound();
    }
}
