using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Development-only seed data so the API can be exercised immediately after
/// startup: roles, an admin user, demo customers, categories, products with
/// inventory, and sample discounts. Idempotent — re-running is a no-op.
/// Production credentials must never be seeded here.
/// </summary>
public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string CustomerRole = "Customer";

    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "Admin@12345";
    public const string CustomerEmail = "customer@example.com";
    public const string CustomerPassword = "Customer@12345";

    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger? logger = null)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await SeedCatalogAsync(context);

        logger?.LogInformation("Development seed data ensured.");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in new[] { AdminRole, CustomerRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                FirstName = "Ada",
                LastName = "Admin",
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AdminRole);
                await userManager.AddToRoleAsync(admin, CustomerRole);
            }
        }

        if (await userManager.FindByEmailAsync(CustomerEmail) is null)
        {
            var customer = new ApplicationUser
            {
                UserName = CustomerEmail,
                Email = CustomerEmail,
                EmailConfirmed = true,
                FirstName = "Casey",
                LastName = "Customer",
                IsActive = true
            };

            var result = await userManager.CreateAsync(customer, CustomerPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customer, CustomerRole);
            }
        }
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext context)
    {
        // Idempotency guard: skip when products already exist.
        if (await context.Products.AnyAsync())
        {
            return;
        }

        var electronics = new Category { Name = "Electronics", Slug = "electronics", Description = "Electronic devices and gadgets", DisplayOrder = 1 };
        var computers = new Category { Name = "Computers", Slug = "computers", Description = "Laptops, desktops and components", DisplayOrder = 2 };
        var accessories = new Category { Name = "Accessories", Slug = "accessories", Description = "Peripherals and accessories", DisplayOrder = 3, ParentCategory = electronics };

        context.Categories.AddRange(electronics, computers, accessories);
        await context.SaveChangesAsync();

        var products = new List<(Product Product, int Stock, Category Category)>
        {
            (new Product { Name = "Mechanical Keyboard TKL", Slug = "mechanical-keyboard-tkl", Description = "Hot-swappable tenkeyless mechanical keyboard with RGB backlight.", SKU = "KB-TKL-001", Price = 89.99m, CompareAtPrice = 109.99m }, 50, accessories),
            (new Product { Name = "Wireless Gaming Mouse", Slug = "wireless-gaming-mouse", Description = "26K DPI wireless gaming mouse with 90 hour battery life.", SKU = "MS-WL-002", Price = 59.99m }, 100, accessories),
            (new Product { Name = "27 inch 4K Monitor", Slug = "27-inch-4k-monitor", Description = "27 inch IPS 4K UHD monitor with 144Hz refresh rate.", SKU = "MN-4K-003", Price = 379.99m, CompareAtPrice = 429.99m }, 25, electronics),
            (new Product { Name = "USB-C Docking Station", Slug = "usb-c-docking-station", Description = "12-in-1 USB-C dock with dual HDMI, ethernet and 100W PD.", SKU = "DK-USC-004", Price = 129.99m }, 40, computers),
            (new Product { Name = "NVMe SSD 2TB", Slug = "nvme-ssd-2tb", Description = "PCIe Gen4 NVMe solid state drive, 7000MB/s sequential read.", SKU = "SD-NV-005", Price = 149.99m, CompareAtPrice = 179.99m }, 75, computers)
        };

        foreach (var (product, stock, category) in products)
        {
            context.Products.Add(product);
            context.ProductCategories.Add(new ProductCategory { Product = product, Category = category });
            context.InventoryItems.Add(new InventoryItem
            {
                Product = product,
                AvailableQuantity = stock,
                ReservedQuantity = 0
            });
            context.InventoryTransactions.Add(new InventoryTransaction
            {
                Product = product,
                Type = InventoryTransactionType.Adjustment,
                Quantity = stock,
                Reference = "Seed",
                Notes = "Initial seed stock"
            });
        }

        // Sample discounts: percentage welcome code and a fixed amount code.
        context.Discounts.AddRange(
            new Discount
            {
                Code = "WELCOME10",
                Name = "Welcome 10%",
                Description = "10% off your entire order",
                Type = DiscountType.Percentage,
                Value = 10m,
                StartsAt = DateTime.UtcNow.AddDays(-1),
                EndsAt = DateTime.UtcNow.AddYears(1),
                UsageLimit = 1000,
                MinimumOrderValue = 50m,
                IsActive = true
            },
            new Discount
            {
                Code = "SAVE20",
                Name = "Save 20",
                Description = "$20 off orders over $150",
                Type = DiscountType.Fixed,
                Value = 20m,
                StartsAt = DateTime.UtcNow.AddDays(-1),
                EndsAt = DateTime.UtcNow.AddYears(1),
                UsageLimit = 500,
                MinimumOrderValue = 150m,
                IsActive = true
            });

        await context.SaveChangesAsync();
    }
}
