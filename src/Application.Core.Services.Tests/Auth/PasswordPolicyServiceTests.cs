using Application.Core.Config;
using Application.Core.Interfaces.Auth;
using Application.Core.Services.Auth;
using NSubstitute;

namespace Application.Core.Services.Tests.Auth;

public sealed class PasswordPolicyServiceTests
{
    private readonly IBreachedPasswordChecker _breached = Substitute.For<IBreachedPasswordChecker>();

    private PasswordPolicyService Create(bool breachCheck = true) =>
        new(new PasswordPolicyConfig { MinLength = 12, MaxLength = 30, BreachCheckEnabled = breachCheck }, _breached);

    [Fact]
    public async Task ValidateAsync_TooShort_Fails()
    {
        var result = await Create().ValidateAsync("short");

        Assert.True(result.IsFailure);
        Assert.Contains("12", result.Error);
        await _breached.DidNotReceiveWithAnyArgs().IsBreachedAsync(default!);
    }

    [Fact]
    public async Task ValidateAsync_TooLong_Fails()
    {
        var result = await Create().ValidateAsync(new string('a', 31));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ValidateAsync_ContainsUsername_FailsCaseInsensitive()
    {
        var result = await Create().ValidateAsync("xxDR.Garciaxxxx", "dr.garcia");

        Assert.True(result.IsFailure);
        Assert.Contains("usuario", result.Error);
    }

    [Fact]
    public async Task ValidateAsync_Breached_Fails()
    {
        _breached.IsBreachedAsync("correct horse battery", Arg.Any<CancellationToken>()).Returns(true);

        var result = await Create().ValidateAsync("correct horse battery");

        Assert.True(result.IsFailure);
        Assert.Contains("filtraciones", result.Error);
    }

    [Fact]
    public async Task ValidateAsync_BreachCheckDisabled_SkipsLookup()
    {
        var result = await Create(breachCheck: false).ValidateAsync("correct horse battery");

        Assert.True(result.IsSuccess);
        await _breached.DidNotReceiveWithAnyArgs().IsBreachedAsync(default!);
    }

    [Fact]
    public async Task ValidateAsync_LongPassphraseWithSpaces_Succeeds()
    {
        var result = await Create().ValidateAsync("correct horse battery", "dr.garcia");

        Assert.True(result.IsSuccess);
    }
}
