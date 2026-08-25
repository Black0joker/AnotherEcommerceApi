using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Auth.Logout;

public record LogoutCommand(string RefreshToken) : ICommand;
