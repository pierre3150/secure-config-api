using Microsoft.EntityFrameworkCore;
using SecureConfigApi.Data;
using SecureConfigApi.Models;

namespace SecureConfigApi.Services;

public interface IConfigService
{
    Task<List<ConfigEntryResponse>> GetAllAsync(string? environment);
    Task<string?> GetDecryptedValueAsync(string key, string environment);
    Task<ConfigEntryResponse> UpsertAsync(ConfigEntryDto dto);
    Task<bool> DeleteAsync(string key, string environment);
}

public class ConfigService : IConfigService
{
    private readonly AppDbContext _db;
    private readonly IEncryptionService _encryption;

    public ConfigService(AppDbContext db, IEncryptionService encryption)
    {
        _db = db;
        _encryption = encryption;
    }

    public async Task<List<ConfigEntryResponse>> GetAllAsync(string? environment)
    {
        var query = _db.ConfigEntries.AsQueryable();
        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(c => c.Environment == environment);

        return await query
            .OrderBy(c => c.Key)
            .Select(c => new ConfigEntryResponse(c.Id, c.Key, c.Environment, c.UpdatedAt))
            .ToListAsync();
    }

    public async Task<string?> GetDecryptedValueAsync(string key, string environment)
    {
        var entry = await _db.ConfigEntries
            .FirstOrDefaultAsync(c => c.Key == key && c.Environment == environment);

        return entry is null ? null : _encryption.Decrypt(entry.EncryptedValue);
    }

    public async Task<ConfigEntryResponse> UpsertAsync(ConfigEntryDto dto)
    {
        var entry = await _db.ConfigEntries
            .FirstOrDefaultAsync(c => c.Key == dto.Key && c.Environment == dto.Environment);

        var encrypted = _encryption.Encrypt(dto.Value);

        if (entry is null)
        {
            entry = new ConfigEntry
            {
                Key = dto.Key,
                Environment = dto.Environment,
                EncryptedValue = encrypted
            };
            _db.ConfigEntries.Add(entry);
        }
        else
        {
            entry.EncryptedValue = encrypted;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return new ConfigEntryResponse(entry.Id, entry.Key, entry.Environment, entry.UpdatedAt);
    }

    public async Task<bool> DeleteAsync(string key, string environment)
    {
        var entry = await _db.ConfigEntries
            .FirstOrDefaultAsync(c => c.Key == key && c.Environment == environment);

        if (entry is null) return false;

        _db.ConfigEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }
}
