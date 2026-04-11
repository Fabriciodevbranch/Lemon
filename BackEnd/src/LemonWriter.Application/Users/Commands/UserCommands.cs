using FluentValidation;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Domain.Interfaces;
using MediatR;

namespace LemonWriter.Application.Users.Commands;

public record RegisterUserCommand(
    string Email,
    string Name,
    string? Password = null,
    string? OAuthProvider = null,
    string? OAuthProviderId = null) : IRequest<Result<UserDto>>;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result<UserDto>.Failure(Error.Custom("EMAIL_TAKEN", "A user with this email already exists."));

        string? passwordHash = null;
        if (request.Password is not null)
        {
            // PBKDF2 with SHA256, 100,000 iterations, 32-byte output
            var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                request.Password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            passwordHash = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        var user = User.Create(request.Email, request.Name, request.OAuthProvider, request.OAuthProviderId, passwordHash);
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email, user.Name, user.OAuthProvider, user.CreatedAt));
    }
}

public record UpdateUserProfileCommand(Guid Id, string Name) : IRequest<Result<UserDto>>;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserProfileCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
            return Result<UserDto>.Failure(Error.NotFound);

        user.UpdateProfile(request.Name);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email, user.Name, user.OAuthProvider, user.CreatedAt));
    }
}
