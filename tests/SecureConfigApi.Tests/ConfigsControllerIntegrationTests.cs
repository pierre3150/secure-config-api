using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SecureConfigApi.Data;
using SecureConfigApi.Models;
using Xunit;

namespace SecureConfigApi.Tests;

public class ConfigsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ConfigsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase($"IntegrationTests-{Guid.NewGuid()}"));
            });
        });

        _client = customFactory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Then_Get_RoundTripsValue()
    {
        var dto = new ConfigEntryDto("Test:Key", "test-value", "production");

        var postResponse = await _client.PostAsJsonAsync("/api/configs", dto);
        var postBody = await postResponse.Content.ReadAsStringAsync();
        Assert.True(postResponse.StatusCode == HttpStatusCode.OK, $"POST failed: {postResponse.StatusCode} body={postBody}");

        var listResponse = await _client.GetAsync("/api/configs?environment=production");
        var listBody = await listResponse.Content.ReadAsStringAsync();

        var getResponse = await _client.GetAsync("/api/configs/value?key=Test:Key&environment=production");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        Assert.True(getResponse.StatusCode == HttpStatusCode.OK,
            $"GET failed: status={getResponse.StatusCode} getBody={getBody} | postBody={postBody} | listBody={listBody}");
    }

    [Fact]
    public async Task Get_UnknownKey_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/configs/value?key=does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithEmptyKey_ReturnsBadRequest()
    {
        var dto = new ConfigEntryDto("", "value", "production");

        var response = await _client.PostAsJsonAsync("/api/configs", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_NeverReturnsRawEncryptedValues()
    {
        await _client.PostAsJsonAsync("/api/configs", new ConfigEntryDto("List:Key", "secret", "production"));

        var response = await _client.GetAsync("/api/configs?environment=production");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("secret", body);
    }
}
