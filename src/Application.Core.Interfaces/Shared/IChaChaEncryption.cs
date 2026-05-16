using Application.Core.DTOs.Encryption.Errors;
using BindSharp;

namespace Application.Core.Interfaces.Shared;

public interface IChaChaEncryption
{
    Result<string, ChaChaEncryptionError> Encrypt(string plaintext);
    Result<string, ChaChaEncryptionError> Encrypt(byte[] byteContent);
    Result<byte[], ChaChaEncryptionError> EncryptToBytes(string plaintext);
    Result<byte[], ChaChaEncryptionError> EncryptToBytes(byte[] byteContent);
    Result<string, ChaChaEncryptionError> Decrypt(string ciphertext);
    Result<string, ChaChaEncryptionError> Decrypt(byte[] ciphertext);
    Result<byte[], ChaChaEncryptionError> DecryptToBytes(string ciphertext);
    Result<byte[], ChaChaEncryptionError> DecryptToBytes(byte[] ciphertext);
}