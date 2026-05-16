namespace Application.Core.Config;

public sealed record DeterministicEncryptionConfig
{
    public required string MasterKey { get; init; }
    public required string IvGenerationKey { get; init; }
}