using Application.Core.DTOs.Encryption.Errors;
using BindSharp;

namespace Application.Core.Interfaces.Shared;

public interface IEncryption
{
    Result<string, ChaChaEncryptionError> Encrypt(string plaintext);
    Result<string, ChaChaEncryptionError> Decrypt(string ciphertext);
}