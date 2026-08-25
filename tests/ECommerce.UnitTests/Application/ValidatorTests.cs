using ECommerce.Application.Features.Auth.Register;
using ECommerce.Application.Features.Products.CreateProduct;
using FluentValidation.TestHelper;

namespace ECommerce.UnitTests.Application;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var command = new RegisterCommand("user@example.com", "Str0ng!Pass", "Jane", "Doe");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("user@@example.com")]
    public void Invalid_Email_Fails(string email)
    {
        var command = new RegisterCommand(email, "Str0ng!Pass", "Jane", "Doe");

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short1!A")]      // 8 chars is the minimum - valid boundary
    public void Minimum_Length_Password_Passes(string password)
    {
        var command = new RegisterCommand("user@example.com", password, "Jane", "Doe");

        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]              // empty
    [InlineData("Ab1!x")]         // too short
    [InlineData("alllowercase1!")]// no uppercase
    [InlineData("ALLUPPERCASE1!")]// no lowercase
    [InlineData("NoDigitsHere!")] // no digit
    [InlineData("NoSpecial1Char")]// no special character
    public void Weak_Password_Fails(string password)
    {
        var command = new RegisterCommand("user@example.com", password, "Jane", "Doe");

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Missing_Names_Fail()
    {
        var command = new RegisterCommand("user@example.com", "Str0ng!Pass", "", "");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }
}

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand Valid() => new(
        Name: "Mechanical Keyboard",
        Description: "A great keyboard",
        SKU: "KB-001",
        Price: 89.99m,
        CompareAtPrice: null,
        CategoryId: null,
        InitialStock: 10);

    [Fact]
    public void Valid_Command_Passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Missing_Name_Fails(string? name)
    {
        var command = Valid() with { Name = name! };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SKU WITH SPACES")]
    [InlineData("sku_special!")]
    public void Invalid_Sku_Fails(string sku)
    {
        var command = Valid() with { SKU = sku };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.SKU);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositive_Price_Fails(decimal price)
    {
        var command = Valid() with { Price = price };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Negative_Initial_Stock_Fails()
    {
        var command = Valid() with { InitialStock = -5 };

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.InitialStock);
    }

    [Fact]
    public void Zero_Initial_Stock_Is_Allowed()
    {
        var command = Valid() with { InitialStock = 0 };

        _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.InitialStock);
    }
}
