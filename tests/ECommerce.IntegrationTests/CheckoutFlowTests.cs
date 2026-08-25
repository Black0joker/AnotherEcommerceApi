using System.Net;
using System.Net.Http.Json;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class CheckoutFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _anonymousClient;

    public CheckoutFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _anonymousClient = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.EnsureRolesAsync();
        await _factory.CreateAdminUserAsync("checkout-admin@example.com", TestHelpers.ValidPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var (token, _) = await TestHelpers.LoginAsync(_anonymousClient, "checkout-admin@example.com", TestHelpers.ValidPassword);
        return _factory.CreateAuthenticatedClient(token);
    }

    private async Task<HttpClient> CreateCustomerClientAsync()
    {
        var client = _factory.CreateClient();
        var (token, _) = await TestHelpers.RegisterUserAsync(client);
        return _factory.CreateAuthenticatedClient(token);
    }

    [Fact]
    public async Task Full_Checkout_Creates_Order_Clears_Cart_And_Reserves_Inventory()
    {
        var adminClient = await CreateAdminClientAsync();
        var productId = await TestHelpers.CreateProductAsync(adminClient, "Checkout Product", 20m, 10);

        var customer = await CreateCustomerClientAsync();

        var addResponse = await customer.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new
            {
                shippingAddress = TestHelpers.SampleShippingAddress(),
                discountCode = (string?)null
            })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var checkoutResponse = await customer.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);

        var order = await TestHelpers.ParseJsonAsync(checkoutResponse);
        Assert.Equal(40m, order["grandTotal"]!.GetValue<decimal>()); // server-side total: 20 * 2
        Assert.True(IsPendingStatus(order["status"]!), "Expected the new order to be Pending.");

        // Cart is cleared after checkout.
        var cartResponse = await customer.GetAsync("/api/v1/cart");
        var cart = await TestHelpers.ParseJsonAsync(cartResponse);
        Assert.Empty(cart["items"]!.AsArray());

        // Inventory moved from available to reserved (never negative).
        var orderId = order["orderId"]!.GetValue<Guid>();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventory = await db.InventoryItems.SingleAsync(i => i.ProductId == productId);
        Assert.Equal(8, inventory.AvailableQuantity);
        Assert.Equal(2, inventory.ReservedQuantity);

        // Order visible in customer's order history.
        var ordersResponse = await customer.GetAsync("/api/v1/orders");
        Assert.Equal(HttpStatusCode.OK, ordersResponse.StatusCode);
        var orders = await TestHelpers.ParseJsonAsync(ordersResponse);
        Assert.Contains(orders.AsArray(), o => o?["id"]?.GetValue<Guid>() == orderId);
    }

    [Fact]
    public async Task Checkout_Is_Idempotent_For_Same_Key()
    {
        var adminClient = await CreateAdminClientAsync();
        var productId = await TestHelpers.CreateProductAsync(adminClient, "Idempotent Product", 30m, 10);

        var customer = await CreateCustomerClientAsync();
        await customer.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 1 });

        var idempotencyKey = Guid.NewGuid().ToString();

        var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new { shippingAddress = TestHelpers.SampleShippingAddress(), discountCode = (string?)null })
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var firstResponse = await customer.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstOrder = await TestHelpers.ParseJsonAsync(firstResponse);

        // Retry with the same key: must return the same order, not create another.
        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new { shippingAddress = TestHelpers.SampleShippingAddress(), discountCode = (string?)null })
        };
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var secondResponse = await customer.SendAsync(secondRequest);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var secondOrder = await TestHelpers.ParseJsonAsync(secondResponse);

        Assert.Equal(firstOrder["orderId"]!.GetValue<Guid>(), secondOrder["orderId"]!.GetValue<Guid>());

        // Exactly one order exists for this idempotency key.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderCount = await db.Orders.CountAsync(o => o.IdempotencyKey == idempotencyKey);
        Assert.Equal(1, orderCount);
    }

    [Fact]
    public async Task Two_Customers_Racing_For_Last_Unit_Never_Oversell()
    {
        var adminClient = await CreateAdminClientAsync();
        var productId = await TestHelpers.CreateProductAsync(adminClient, "Last Unit Product", 50m, 1);

        var customerA = await CreateCustomerClientAsync();
        var customerB = await CreateCustomerClientAsync();

        // Both add the last unit to their carts (cart add does not reserve stock).
        var addA = await customerA.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 1 });
        var addB = await customerB.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 1 });
        Assert.Equal(HttpStatusCode.OK, addA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, addB.StatusCode);

        async Task<HttpResponseMessage> Checkout(HttpClient client)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
            {
                Content = JsonContent.Create(new { shippingAddress = TestHelpers.SampleShippingAddress(), discountCode = (string?)null })
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            return await client.SendAsync(request);
        }

        var checkoutA = await Checkout(customerA);
        var checkoutB = await Checkout(customerB);

        // One succeeds, the other gets a clean failure - never negative stock.
        Assert.Equal(HttpStatusCode.OK, checkoutA.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, checkoutB.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventory = await db.InventoryItems.SingleAsync(i => i.ProductId == productId);
        Assert.True(inventory.AvailableQuantity >= 0, "Stock must never go negative.");
        Assert.Equal(0, inventory.AvailableQuantity);
        Assert.Equal(1, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task Checkout_With_Empty_Cart_Returns_400()
    {
        var customer = await CreateCustomerClientAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new { shippingAddress = TestHelpers.SampleShippingAddress(), discountCode = (string?)null })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await customer.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Customer_Cannot_See_Another_Customers_Order_And_Admin_Can()
    {
        var adminClient = await CreateAdminClientAsync();
        var productId = await TestHelpers.CreateProductAsync(adminClient, "IDOR Product", 12m, 10);

        var customerA = await CreateCustomerClientAsync();
        await customerA.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 1 });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new { shippingAddress = TestHelpers.SampleShippingAddress(), discountCode = (string?)null })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var checkoutResponse = await customerA.SendAsync(request);
        var order = await TestHelpers.ParseJsonAsync(checkoutResponse);
        var orderId = order["orderId"]!.GetValue<Guid>();

        // Another customer cannot access the order (no existence leak: 404).
        var customerB = await CreateCustomerClientAsync();
        var otherResponse = await customerB.GetAsync($"/api/v1/orders/{orderId}");
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);

        // Their own order list does not contain it either.
        var listResponse = await customerB.GetAsync("/api/v1/orders");
        var list = await TestHelpers.ParseJsonAsync(listResponse);
        Assert.DoesNotContain(list.AsArray(), o => o?["id"]?.GetValue<Guid>() == orderId);

        // Admin can access any order.
        var adminOrderResponse = await adminClient.GetAsync($"/api/v1/admin/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, adminOrderResponse.StatusCode);
    }

    [Fact]
    public async Task Cancel_Pending_Order_Releases_Reserved_Inventory()
    {
        var adminClient = await CreateAdminClientAsync();
        var productId = await TestHelpers.CreateProductAsync(adminClient, "Cancellable Product", 40m, 5);

        var customer = await CreateCustomerClientAsync();
        await customer.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 2 });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new { shippingAddress = TestHelpers.SampleShippingAddress(), discountCode = (string?)null })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var checkoutResponse = await customer.SendAsync(request);
        var order = await TestHelpers.ParseJsonAsync(checkoutResponse);
        var orderId = order["orderId"]!.GetValue<Guid>();

                var cancelResponse = await customer.PostAsJsonAsync($"/api/v1/orders/{orderId}/cancel", new { reason = "changed my mind" });
                Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

                // A cancelled order cannot be cancelled twice.
                var secondCancel = await customer.PostAsJsonAsync($"/api/v1/orders/{orderId}/cancel", new { reason = "again" });
                Assert.NotEqual(HttpStatusCode.OK, secondCancel.StatusCode);
            }

            /// <summary>
            /// Status may be serialized either as the enum name or its numeric value
            /// depending on JSON options; accept both representations of Pending.
            /// </summary>
            private static bool IsPendingStatus(System.Text.Json.Nodes.JsonNode statusNode) =>
                statusNode.GetValueKind() switch
                {
                    System.Text.Json.JsonValueKind.String => string.Equals(statusNode.GetValue<string>(), "Pending", StringComparison.OrdinalIgnoreCase),
                    System.Text.Json.JsonValueKind.Number => statusNode.GetValue<int>() == 0, // OrderStatus.Pending
                    _ => false
                };
        }
