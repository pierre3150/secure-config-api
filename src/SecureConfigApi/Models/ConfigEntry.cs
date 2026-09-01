using System.ComponentModel.DataAnnotations;

namespace SecureConfigApi.Models;

public class ConfigEntry
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string EncryptedValue { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Environment { get; set; } = "production";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public record ConfigEntryDto(string Key, string Value, string Environment);
public record ConfigEntryResponse(int Id, string Key, string Environment, DateTime UpdatedAt);
