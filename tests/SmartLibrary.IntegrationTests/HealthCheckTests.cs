using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartLibrary.IntegrationTests;

public class HealthCheckTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthCheckTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_healthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
