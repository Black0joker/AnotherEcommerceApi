using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Shared helpers for exercising the API over HTTP in integration tests.
/// </summary>
internal static class TestHelpers
{
    public const string ValidPassword = "Test@12345";

    public static async Task<JsonNode> ParseJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(body) ?? throw new InvalidOperationException($"Empty JSON body. Status: {(int)response.StatusCode}");
    }

    /// <summary>
    /// Registers a brand-new user and returns (accessToken, refreshToken).
    /// </summary>
    public static async Task<(string AccessToken, string RefreshToken)> RegisterUserAsync(
        HttpClient client, string? email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}"[..20] + "@example.com";

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = ValidPassword,
            firstName = "Test",
            lastName = "User"
        });

        response.EnsureSuccessStatusCode();

        var json = await ParseJsonAsync(response);
        return (json["accessToken"]!.GetValue<string>(), json["refreshToken"]!.GetValue<string>());
    }

    /// <summary>
    /// Logs in and returns (accessToken, refreshToken).
    /// </summary>
    public static async Task<(string AccessToken, string RefreshToken)> LoginAsync(
        HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();

        var json = await ParseJsonAsync(response);
        return (json["accessToken"]!.GetValue<string>(), json["refreshToken"]!.GetValue<string>());
    }

    /// <summary>
    /// Creates a client with the given bearer token attached.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(this CustomWebApplicationFactory factory, string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Creates a product as admin and returns the new product id.
    /// </summary>
    public static async Task<Guid> CreateProductAsync(
        HttpClient adminClient,
        string name,
        decimal price,
        int initialStock,
        string? sku = null)
    {
        sku ??= $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var response = await adminClient.PostAsJsonAsync("/api/v1/products", new
        {
            name,
            description = $"Description for {name}",
            sku,
            price,
            compareAtPrice = (decimal?)null,
            categoryId = (Guid?)null,
            initialStock
        });

        response.EnsureSuccessStatusCode();
        var json = await ParseJsonAsync(response);
        return json["id"]!.GetValue<Guid>();
    }

    public static object SampleShippingAddress() => new
    {
        firstName = "Jane",
        lastName = "Doe",
        streetLine1 = "123 Test Street",
        streetLine2 = (string?)null,
        city = "Testville",
        state = "TS",
        postalCode = "12345",
        country = "US",
        phoneNumber = (string?)null
    };
}
