using Application.Core.DTOs.Encryption.Errors;
using BindSharp;

namespace Application.Core.Interfaces.Auth;

public interface IPassword
{
    Result<string, ChaChaEncryptionError> HashPassword(string password);
    Result<bool, ChaChaEncryptionError> VerifyPassword(string password, string passwordHash);
}