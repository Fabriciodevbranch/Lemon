namespace LemonWriter.Application.Common.Interfaces;

public interface IAuthService
{
    Task<bool> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);
}
