namespace Infrastructure.Core.DTOs.PrivacyNotice;

public abstract record PrivacyNoticeRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record PublishPrivacyNoticeError(string? Details = null, Exception? Exception = null)
    : PrivacyNoticeRepositoryError("Error inesperado al publicar el aviso de privacidad.", Details, Exception);

public sealed record GetPrivacyNoticeError(string? Details = null, Exception? Exception = null)
    : PrivacyNoticeRepositoryError("Error inesperado al recuperar el aviso de privacidad.", Details, Exception);

public sealed record AcknowledgePrivacyNoticeError(string? Details = null, Exception? Exception = null)
    : PrivacyNoticeRepositoryError("Error inesperado al registrar la conformidad con el aviso de privacidad.", Details, Exception);
