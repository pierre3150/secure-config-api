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
        // AES-CBC/PKCS7 padding means a wrong key usually (but not always,
        // ~1/256 chance) throws a CryptographicException on invalid padding.
        // The security property we actually care about is that the wrong key
        // never recovers the original plaintext - whether via exception or garbage.
        var serviceA = CreateService("key-one");
        var serviceB = CreateService("key-two");
        const string original = "sensitive-data";

        var encrypted = serviceA.Encrypt(original);

        string? decrypted = null;
        var exception = Record.Exception(() => decrypted = serviceB.Decrypt(encrypted));

        if (exception is null)
        {
            Assert.NotEqual(original, decrypted);
        }
        else
        {
            Assert.IsAssignableFrom<Exception>(exception);
        }
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
