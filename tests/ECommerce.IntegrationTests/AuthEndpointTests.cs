using System.Net;
using System.Net.Http.Json;

namespace ECommerce.IntegrationTests;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.EnsureRolesAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_Returns_Tokens()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"reg-{Guid.NewGuid():N}"[..16] + "@example.com",
            password = TestHelpers.ValidPassword,
            firstName = "Jane",
            lastName = "Doe"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await TestHelpers.ParseJsonAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(json["accessToken"]!.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(json["refreshToken"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Register_With_Weak_Password_Returns_400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "weak@example.com",
            password = "weak",
            firstName = "Jane",
            lastName = "Doe"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Valid_Credentials_Returns_Tokens()
    {
        var client = _factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}"[..16] + "@example.com";
        await TestHelpers.RegisterUserAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = TestHelpers.ValidPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ParseJsonAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(json["accessToken"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_401()
    {
        var client = _factory.CreateClient();
        var email = $"wrong-{Guid.NewGuid():N}"[..16] + "@example.com";
        await TestHelpers.RegisterUserAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "Wrong@99999"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_Without_Token_Returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_With_Token_Returns_Profile()
    {
        var client = _factory.CreateClient();
        var email = $"me-{Guid.NewGuid():N}"[..16] + "@example.com";
        var (accessToken, _) = await TestHelpers.RegisterUserAsync(client, email);

        var authedClient = _factory.CreateAuthenticatedClient(accessToken);
        var response = await authedClient.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ParseJsonAsync(response);
        Assert.Equal(email, json["email"]!.GetValue<string>());
    }

    [Fact]
    public async Task Refresh_Rotates_Tokens_And_Revokes_Old_Refresh_Token()
    {
        var client = _factory.CreateClient();
        var email = $"refresh-{Guid.NewGuid():N}"[..16] + "@example.com";
        var (accessToken, refreshToken) = await TestHelpers.RegisterUserAsync(client, email);

        // First refresh succeeds and issues new tokens.
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            accessToken,
            refreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var rotated = await TestHelpers.ParseJsonAsync(refreshResponse);
        var newRefreshToken = rotated["refreshToken"]!.GetValue<string>();
        Assert.NotEqual(refreshToken, newRefreshToken);

        // Replaying the old refresh token must fail (rotation revokes it).
        var replayResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            accessToken,
            refreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_Revokes_Session()
    {
        var client = _factory.CreateClient();
        var email = $"logout-{Guid.NewGuid():N}"[..16] + "@example.com";
        var (accessToken, refreshToken) = await TestHelpers.RegisterUserAsync(client, email);

        var authedClient = _factory.CreateAuthenticatedClient(accessToken);
        var logoutResponse = await authedClient.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // The revoked refresh token can no longer be used.
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            accessToken,
            refreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
