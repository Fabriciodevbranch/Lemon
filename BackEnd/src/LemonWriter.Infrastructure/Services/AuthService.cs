using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly LemonDbContext _context;

    public AuthService(LemonDbContext context) => _context = context;

    public async Task<bool> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return false;

        return VerifyPbkdf2Password(password, user.PasswordHash);
    }

    private static bool VerifyPbkdf2Password(string password, string storedHash)
    {
        // Format: "base64salt.base64hash" (PBKDF2/SHA256, 100k iterations, 32-byte key)
        var parts = storedHash.Split('.');
        if (parts.Length != 2)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);

            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
            var actualHash = pbkdf2.GetBytes(32);

            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
