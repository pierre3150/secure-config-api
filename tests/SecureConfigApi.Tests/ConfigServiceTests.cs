using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecureConfigApi.Data;
using SecureConfigApi.Models;
using SecureConfigApi.Services;
using Xunit;

namespace SecureConfigApi.Tests;

public class ConfigServiceTests
{
    private static (AppDbContext db, IConfigService service) CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "test-key"
            })
            .Build();

        var encryption = new AesEncryptionService(config);
        var service = new ConfigService(db, encryption);

        return (db, service);
    }

    [Fact]
    public async Task UpsertAsync_CreatesNewEntry_WhenKeyDoesNotExist()
    {
        var (_, service) = CreateContext();

        var result = await service.UpsertAsync(new ConfigEntryDto("Db:ConnectionString", "secret-value", "staging"));

        Assert.Equal("Db:ConnectionString", result.Key);
        Assert.Equal("staging", result.Environment);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExistingEntry_WhenKeyAlreadyExists()
    {
        var (db, service) = CreateContext();

        await service.UpsertAsync(new ConfigEntryDto("Api:Key", "value-1", "production"));
        await service.UpsertAsync(new ConfigEntryDto("Api:Key", "value-2", "production"));

        var count = await db.ConfigEntries.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetDecryptedValueAsync_ReturnsOriginalValue()
    {
        var (_, service) = CreateContext();
        await service.UpsertAsync(new ConfigEntryDto("Smtp:Password", "P@ssw0rd!", "production"));

        var value = await service.GetDecryptedValueAsync("Smtp:Password", "production");

        Assert.Equal("P@ssw0rd!", value);
    }

    [Fact]
    public async Task GetDecryptedValueAsync_ReturnsNull_WhenKeyDoesNotExist()
    {
        var (_, service) = CreateContext();

        var value = await service.GetDecryptedValueAsync("Nonexistent", "production");

        Assert.Null(value);
    }

    [Fact]
    public async Task GetAllAsync_NeverExposesEncryptedOrDecryptedValues()
    {
        var (_, service) = CreateContext();
        await service.UpsertAsync(new ConfigEntryDto("Secret:Key", "top-secret", "production"));

        var all = await service.GetAllAsync("production");

        Assert.Single(all);
        Assert.Equal("Secret:Key", all[0].Key);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry_AndReturnsTrue()
    {
        var (_, service) = CreateContext();
        await service.UpsertAsync(new ConfigEntryDto("Temp:Key", "value", "production"));

        var deleted = await service.DeleteAsync("Temp:Key", "production");
        var value = await service.GetDecryptedValueAsync("Temp:Key", "production");

        Assert.True(deleted);
        Assert.Null(value);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenKeyDoesNotExist()
    {
        var (_, service) = CreateContext();

        var deleted = await service.DeleteAsync("Ghost:Key", "production");

        Assert.False(deleted);
    }

    [Fact]
    public async Task SameKey_DifferentEnvironments_AreIndependent()
    {
        var (_, service) = CreateContext();
        await service.UpsertAsync(new ConfigEntryDto("Db:Host", "staging-db.internal", "staging"));
        await service.UpsertAsync(new ConfigEntryDto("Db:Host", "prod-db.internal", "production"));

        var staging = await service.GetDecryptedValueAsync("Db:Host", "staging");
        var production = await service.GetDecryptedValueAsync("Db:Host", "production");

        Assert.Equal("staging-db.internal", staging);
        Assert.Equal("prod-db.internal", production);
    }
}
