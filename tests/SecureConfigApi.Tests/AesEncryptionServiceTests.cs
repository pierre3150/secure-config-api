using Microsoft.Extensions.Configuration;
using SecureConfigApi.Services;
using Xunit;

namespace SecureConfigApi.Tests;

public class AesEncryptionServiceTests
{
    private static AesEncryptionService CreateService(string key = "unit-test-key-123")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = key
            })
            .Build();

        return new AesEncryptionService(config);
    }

    [Fact]
    public void Encrypt_Then_Decrypt_ReturnsOriginalPlainText()
    {
        var service = CreateService();
        const string original = "Server=sql01;Database=Prod;Password=S3cret!";

        var encrypted = service.Encrypt(original);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertext_ForSamePlainText()
    {
        var service = CreateService();
        const string original = "same-value";

        var encrypted1 = service.Encrypt(original);
        var encrypted2 = service.Encrypt(original);

        Assert.NotEqual(encrypted1, encrypted2); // random IV each time
    }

    [Fact]
    public void Encrypt_ReturnsBase64String()
    {
        var service = CreateService();
        var encrypted = service.Encrypt("hello world");

        var act = () => Convert.FromBase64String(encrypted);
        var exception = Record.Exception(act);

        Assert.Null(exception);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsOrProducesGarbage()
    {
        var serviceA = CreateService("key-one");
        var serviceB = CreateService("key-two");

        var encrypted = serviceA.Encrypt("sensitive-data");

        Assert.ThrowsAny<Exception>(() => serviceB.Decrypt(encrypted));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("a very long connection string with special chars !@#$%^&*()_+-=")]
    public void Encrypt_Decrypt_RoundTrips_ForVariousInputs(string value)
    {
        var service = CreateService();

        var encrypted = service.Encrypt(value);
        var decrypted = service.Decrypt(encrypted);

        Assert.Equal(value, decrypted);
    }
}
