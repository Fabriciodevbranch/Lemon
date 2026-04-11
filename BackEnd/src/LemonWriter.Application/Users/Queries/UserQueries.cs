using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using MediatR;

namespace LemonWriter.Application.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository) => _userRepository = userRepository;

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
            return Result<UserDto>.Failure(Error.NotFound);

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email, user.Name, user.OAuthProvider, user.CreatedAt));
    }
}

public record GetUserByEmailQuery(string Email) : IRequest<Result<UserDto>>;

public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserByEmailQueryHandler(IUserRepository userRepository) => _userRepository = userRepository;

    public async Task<Result<UserDto>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return Result<UserDto>.Failure(Error.NotFound);

        return Result<UserDto>.Success(new UserDto(user.Id, user.Email, user.Name, user.OAuthProvider, user.CreatedAt));
    }
}
