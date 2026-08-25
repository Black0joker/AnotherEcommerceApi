using ECommerce.Application.Common;

namespace ECommerce.UnitTests.Application;

public class ResultTests
{
    [Fact]
    public void Success_Result_Has_No_Error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_Result_Carries_Error()
    {
        var error = Error.Validation("Test.Error", "message");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_Success_Exposes_Value()
    {
        var result = Result.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Generic_Failure_Value_Access_Throws()
    {
        var result = Result.Failure<int>(Error.NotFound("X", "not found"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Constructor_Rejects_Inconsistent_State()
    {
        Assert.Throws<InvalidOperationException>(() => new Result<int>(0, true, Error.None));
        Assert.Throws<InvalidOperationException>(() => new Result<int>(0, false, null));
    }
}
