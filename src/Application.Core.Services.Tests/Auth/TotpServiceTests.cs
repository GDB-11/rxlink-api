using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using NSubstitute;

namespace Application.Core.Services.Tests.Auth;

public sealed class TotpServiceTests
{
    // RFC 6238 appendix B secret "12345678901234567890" in base32; at T=59s step 1 the 6-digit code is 287082.
    private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    private readonly ITimeProvider _timeProvider = Substitute.For<ITimeProvider>();
    private readonly TotpService _sut;

    public TotpServiceTests()
    {
        _sut = new TotpService(_timeProvider);
        _timeProvider.UtcNow.Returns(DateTime.UnixEpoch.AddSeconds(59));
    }

    [Fact]
    public void Validate_RfcTestVector_ReturnsStep()
    {
        Assert.Equal(1L, _sut.Validate(RfcSecret, "287082"));
    }

    [Fact]
    public void Validate_WrongCode_ReturnsNull()
    {
        Assert.Null(_sut.Validate(RfcSecret, "000000"));
    }

    [Theory]
    [InlineData("28708")]
    [InlineData("2870822")]
    [InlineData("28708a")]
    [InlineData("")]
    public void Validate_MalformedCode_ReturnsNull(string code)
    {
        Assert.Null(_sut.Validate(RfcSecret, code));
    }

    [Fact]
    public void Validate_CodeFromPreviousStep_AcceptedWithinDrift()
    {
        _timeProvider.UtcNow.Returns(DateTime.UnixEpoch.AddSeconds(59 + 30)); // current step 2, code is step 1

        Assert.Equal(1L, _sut.Validate(RfcSecret, "287082"));
    }

    [Fact]
    public void Validate_CodeTwoStepsOld_Rejected()
    {
        _timeProvider.UtcNow.Returns(DateTime.UnixEpoch.AddSeconds(59 + 60));

        Assert.Null(_sut.Validate(RfcSecret, "287082"));
    }

    [Fact]
    public void GenerateSecret_ProducesValidatableBase32Secret()
    {
        string secret = _sut.GenerateSecret();

        Assert.Equal(32, secret.Length);
        Assert.Matches("^[A-Z2-7]{32}$", secret);
    }

    [Fact]
    public void BuildOtpauthUri_ContainsIssuerSecretAndAccount()
    {
        string uri = _sut.BuildOtpauthUri("ABC", "RxLink", "dr@x.com");

        Assert.StartsWith("otpauth://totp/RxLink:dr%40x.com?secret=ABC&issuer=RxLink", uri);
    }
}
