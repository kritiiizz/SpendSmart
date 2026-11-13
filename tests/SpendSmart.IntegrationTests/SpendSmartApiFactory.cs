using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SpendSmart.Api.Authentication;
using Testcontainers.PostgreSql;

namespace SpendSmart.IntegrationTests;

/// <summary>
/// Boots the real application against a throwaway PostgreSQL container (Docker required).
/// One container is shared by all tests; each test registers its own user, so tests stay isolated.
/// </summary>
public sealed class SpendSmartApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public async Task InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("RecurringExpenses:Enabled", "false");
        builder.UseSetting("RateLimiting:AuthPermitsPerMinute", "10000");
    }

    /// <summary>Registers a brand-new user and returns a client that sends their bearer token.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        var email = $"user-{Guid.NewGuid():N}@tests.dev";
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123" });
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<SpendSmartApiFactory>
{
    public const string Name = "api";
}
