namespace ECommerce.Application.Common;

public record Error(string Code, string Message)
{
    public static Error None => new(string.Empty, string.Empty);

    public static Error NotFound(string code, string message) => new(code, message);
    public static Error Validation(string code, string message) => new(code, message);
    public static Error Conflict(string code, string message) => new(code, message);
    public static Error Internal(string code, string message) => new(code, message);
}
