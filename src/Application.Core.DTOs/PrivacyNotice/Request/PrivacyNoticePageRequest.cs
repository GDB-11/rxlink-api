namespace Application.Core.DTOs.PrivacyNotice.Request;

public sealed record PrivacyNoticePageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
