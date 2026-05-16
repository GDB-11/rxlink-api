using Application.Core.DTOs.Encryption.Errors;
using BindSharp;

namespace Application.Core.Interfaces.Shared;

public interface IDeterministicEncryption
{
    Result<string, DeterministicEncryptionError> Encrypt(string plaintext);
    Result<string, DeterministicEncryptionError> Decrypt(string ciphertext);
}