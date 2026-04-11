using LemonWriter.Application.Common.Errors;
using Xunit;
using FluentAssertions;

namespace LemonWriter.Application.Tests;

public class ResultTests
{
    [Fact]
    public void Result_Success_ShouldBeSuccessful()
    {
        var result = Result<string>.Success("hello");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Failure_ShouldBeFailure()
    {
        var result = Result<string>.Failure(Error.NotFound);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NotFound);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Result_Match_ShouldCallCorrectBranch()
    {
        var success = Result<int>.Success(42);
        var value = success.Match(v => v * 2, e => -1);
        value.Should().Be(84);

        var failure = Result<int>.Failure(Error.NotFound);
        var fallback = failure.Match(v => v * 2, e => -1);
        fallback.Should().Be(-1);
    }
}
