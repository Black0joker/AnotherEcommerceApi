using System.Net;
using System.Net.Http.Json;

namespace ECommerce.IntegrationTests;

public class CatalogAndCartTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _anonymousClient;

    public CatalogAndCartTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _anonymousClient = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureRolesAsync();
        await _factory.CreateAdminUserAsync("catalog-admin@example.com", TestHelpers.ValidPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var (token, _) = await TestHelpers.LoginAsync(_anonymousClient, "catalog-admin@example.com", TestHelpers.ValidPassword);
        return _factory.CreateAuthenticatedClient(token);
    }

    [Fact]
    public async Task Products_List_Is_Public_And_Paged()
    {
        var adminClient = await CreateAdminClientAsync();
        await TestHelpers.CreateProductAsync(adminClient, "Public List Product", 25m, 10);

        var response = await _anonymousClient.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ParseJsonAsync(response);
        Assert.NotNull(json["items"]);
        Assert.True(json["totalCount"]!.GetValue<int>() >= 1);
        Assert.True(json["totalPages"]!.GetValue<int>() >= 1);
    }

    [Fact]
    public async Task Product_Details_Can_Be_Fetched_Anonymously()
    {
        var adminClient = await CreateAdminClientAsync();
        var productId = await TestHelpers.CreateProductAsync(adminClient, "Detail Product", 42m, 5);

        var response = await _anonymousClient.GetAsync($"/api/v1/products/{productId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Creating_Product_Requires_Admin()
    {
        var customerClient = _factory.CreateClient();
        var email = $"notadmin-{Guid.NewGuid():N}"[..16] + "@example.com";
        var (token, _) = await TestHelpers.RegisterUserAsync(customerClient, email);
        var authed = _factory.CreateAuthenticatedClient(token);

        var response = await authed.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Sneaky Product",
            description = "Should not be created",
            sku = "SNEAK-1",
            price = 10m,
            compareAtPrice = (decimal?)null,
            categoryId = (Guid?)null,
            initialStock = 5
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cart_Requires_Authentication()
    {
        var response = await _anonymousClient.GetAsync("/api/v1/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Add_Cart_Item_With_Missing_Product_Returns_404()
    {
        var client = _factory.CreateClient();
        var (token, _) = await TestHelpers.RegisterUserAsync(client);
        var authed = _factory.CreateAuthenticatedClient(token);

        var response = await authed.PostAsJsonAsync("/api/v1/cart/items", new
        {
            productId = Guid.NewGuid(),
            quantity = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_Cart_Item_Beyond_Stock_Returns_400()
    {
        var adminClient = await CreateAdminClientAsync();
        var productId = await TestHelpers.CreateProductAsync(adminClient, "Scarce Product", 15m, 2);

        var client = _factory.CreateClient();
        var (token, _) = await TestHelpers.RegisterUserAsync(client);
        var authed = _factory.CreateAuthenticatedClient(token);

        var response = await authed.PostAsJsonAsync("/api/v1/cart/items", new
        {
            productId,
            quantity = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_Cart_Item_Then_Get_Cart_Returns_Server_Computed_Totals()
    {
        var adminClient = await CreateAdminClientAsync();
        var productId = await TestHelpers.CreateProductAsync(adminClient, "Cart Product", 10.50m, 20);

        var client = _factory.CreateClient();
        var (token, _) = await TestHelpers.RegisterUserAsync(client);
        var authed = _factory.CreateAuthenticatedClient(token);

        var addResponse = await authed.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 3 });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var cartResponse = await authed.GetAsync("/api/v1/cart");
        Assert.Equal(HttpStatusCode.OK, cartResponse.StatusCode);

        var cart = await TestHelpers.ParseJsonAsync(cartResponse);
        Assert.Equal(31.50m, cart["total"]!.GetValue<decimal>()); // server-side price * qty, never client totals
        var item = Assert.Single(cart["items"]!.AsArray());
        Assert.Equal(10.50m, item!["unitPrice"]!.GetValue<decimal>());
    }
}
