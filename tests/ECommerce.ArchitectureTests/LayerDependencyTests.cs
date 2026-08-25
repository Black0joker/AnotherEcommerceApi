using System.Reflection;

namespace ECommerce.ArchitectureTests;

/// <summary>
/// Reflection-based architecture guardrails. These tests encode the Clean
/// Architecture dependency direction from the project plan:
///
///   Api -> Application -> Domain
///   Infrastructure -> Application / Domain
///
/// Domain and Application must never depend upward on Infrastructure/Api or on
/// ASP.NET Core / EF Core concerns.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(ECommerce.Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly Application = typeof(ECommerce.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(ECommerce.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static IEnumerable<string> ReferencedAssemblyNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!);

    // -----------------------------------------------------------------
    // Domain layer rules
    // -----------------------------------------------------------------

    [Fact]
    public void Domain_Does_Not_Reference_Application()
    {
        Assert.DoesNotContain("ECommerce.Application", ReferencedAssemblyNames(Domain));
    }

    [Fact]
    public void Domain_Does_Not_Reference_Infrastructure()
    {
        Assert.DoesNotContain("ECommerce.Infrastructure", ReferencedAssemblyNames(Domain));
    }

    [Fact]
    public void Domain_Does_Not_Reference_Api()
    {
        Assert.DoesNotContain("ECommerce.Api", ReferencedAssemblyNames(Domain));
    }

    [Fact]
    public void Domain_Does_Not_Depend_On_AspNetCore_Or_EFCore()
    {
        var forbidden = ReferencedAssemblyNames(Domain)
            .Where(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)
                           || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));

        Assert.True(!forbidden.Any(),
            $"Domain references forbidden frameworks: {string.Join(", ", forbidden)}");
    }

    // -----------------------------------------------------------------
    // Application layer rules
    // -----------------------------------------------------------------

    [Fact]
    public void Application_Does_Not_Reference_Infrastructure()
    {
        Assert.DoesNotContain("ECommerce.Infrastructure", ReferencedAssemblyNames(Application));
    }

    [Fact]
    public void Application_Does_Not_Reference_Api()
    {
        Assert.DoesNotContain("ECommerce.Api", ReferencedAssemblyNames(Application));
    }

    [Fact]
    public void Application_Does_Not_Reference_EFCore()
    {
        var forbidden = ReferencedAssemblyNames(Application)
            .Where(name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));

        Assert.True(!forbidden.Any(),
            $"Application references EF Core directly: {string.Join(", ", forbidden)}");
    }

    [Fact]
    public void Application_Does_Not_Reference_AspNetCore_Mvc()
    {
        var forbidden = ReferencedAssemblyNames(Application)
            .Where(name => name.Equals("Microsoft.AspNetCore.Mvc.Core", StringComparison.OrdinalIgnoreCase)
                           || name.Equals("Microsoft.AspNetCore.Mvc.Abstractions", StringComparison.OrdinalIgnoreCase));

        Assert.True(!forbidden.Any(),
            $"Application references ASP.NET Core MVC: {string.Join(", ", forbidden)}");
    }

    // -----------------------------------------------------------------
    // Infrastructure / Api wiring rules
    // -----------------------------------------------------------------

    [Fact]
    public void Infrastructure_Does_Not_Reference_Api()
    {
        Assert.DoesNotContain("ECommerce.Api", ReferencedAssemblyNames(Infrastructure));
    }

    [Fact]
    public void Api_References_Application_And_Infrastructure()
    {
        var references = ReferencedAssemblyNames(Api);

        Assert.Contains("ECommerce.Application", references);
        Assert.Contains("ECommerce.Infrastructure", references);
    }
}
