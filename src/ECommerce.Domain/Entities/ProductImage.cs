using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;
}
