using System.Reflection;
using ECommerce.Application.Common;
using MediatR;

namespace ECommerce.ArchitectureTests;

/// <summary>
/// Structural conventions that keep the modular monolith consistent:
/// thin controllers, MediatR feature organization, and repository boundaries.
/// </summary>
public class ConventionTests
{
    private static readonly Assembly Application = typeof(ECommerce.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(ECommerce.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact]
    public void Controllers_Live_Only_In_The_Api_Project()
    {
        var nonApiControllers = new[] { Application, Infrastructure }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

        Assert.True(nonApiControllers.Count == 0,
            $"Controllers found outside the Api project: {string.Join(", ", nonApiControllers.Select(t => t.FullName))}");
    }

    [Fact]
    public void Controllers_Are_Thin_And_Do_Not_Access_EF_DbContext_Directly()
    {
        var controllers = Api.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal));

        foreach (var controller in controllers)
        {
            var fields = controller.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Select(f => f.FieldType.FullName ?? string.Empty);

            Assert.True(!fields.Any(f => f.Contains("ApplicationDbContext")),
                $"{controller.Name} depends on ApplicationDbContext directly; controllers must go through application features.");
        }
    }

    [Fact]
    public void Every_Command_Or_Query_Has_Exactly_One_Handler()
    {
        var requestTypes = Application.GetTypes()
            .Where(t => t is { IsInterface: false, IsAbstract: false }
                        && t.GetInterfaces().Any(i =>
                            (i.IsGenericType && (
                                i.GetGenericTypeDefinition() == typeof(ICommand<>) ||
                                i.GetGenericTypeDefinition() == typeof(IQuery<>))) ||
                            i == typeof(ICommand)))
            .ToList();

        var handlerTypes = Application.GetTypes()
            .Where(t => t is { IsInterface: false, IsAbstract: false }
                        && t.GetInterfaces().Any(i => i.IsGenericType && (
                            i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                            i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                            i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))))
            .ToList();

        foreach (var requestType in requestTypes)
        {
            var handlers = handlerTypes.Where(h => h.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericArguments().First() == requestType)).ToList();

            Assert.True(handlers.Count == 1,
                $"Request {requestType.Name} has {handlers.Count} handlers; expected exactly one.");
        }
    }

    [Fact]
    public void Repository_Interfaces_Are_Defined_In_Application_Abstractions()
    {
        var repositoryInterfaces = Application.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository", StringComparison.Ordinal));

        foreach (var repo in repositoryInterfaces)
        {
            Assert.Equal("ECommerce.Application.Abstractions", repo.Namespace);
        }
    }

    [Fact]
    public void EF_DbContext_Lives_Only_In_Infrastructure()
    {
        var dbContextTypes = new[] { Application, Api }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.Name.EndsWith("DbContext", StringComparison.Ordinal))
            .ToList();

        Assert.True(dbContextTypes.Count == 0,
            "DbContext types must live in the Infrastructure layer only.");

        Assert.Contains(Infrastructure.GetTypes(),
            t => t.Name == "ApplicationDbContext");
    }
}
