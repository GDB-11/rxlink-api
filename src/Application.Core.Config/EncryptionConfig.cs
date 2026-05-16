namespace Application.Core.Config;

public sealed record EncryptionConfig
{
    public required string MasterKey { get; init; }
}