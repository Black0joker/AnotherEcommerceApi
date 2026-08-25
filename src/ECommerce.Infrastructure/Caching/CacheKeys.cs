namespace ECommerce.Infrastructure.Caching;

/// <summary>
/// Centralized cache key definitions.
/// </summary>
public static class CacheKeys
{
    public const string ProductsPrefix = "products:";
    public const string CategoriesPrefix = "categories:";

    public static string ProductById(Guid id) => $"{ProductsPrefix}detail:{id}";
    public static string ProductBySlug(string slug) => $"{ProductsPrefix}slug:{slug}";
    public static string ProductList(string query) => $"{ProductsPrefix}list:{query}";
    public static string ProductRelated(Guid id) => $"{ProductsPrefix}related:{id}";

    public static string CategoryById(Guid id) => $"{CategoriesPrefix}detail:{id}";
    public static string CategoryList() => $"{CategoriesPrefix}list";

    public static TimeSpan ProductDetailExpiration { get; } = TimeSpan.FromMinutes(15);
    public static TimeSpan ProductListExpiration { get; } = TimeSpan.FromMinutes(5);
    public static TimeSpan CategoryExpiration { get; } = TimeSpan.FromMinutes(30);
}
