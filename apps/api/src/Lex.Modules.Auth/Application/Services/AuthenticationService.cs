using Lex.Modules.Auth.Application.Contracts;
using Lex.Modules.Auth.Application.DTOs;
using Lex.Modules.Auth.Domain.Repositories;
using Lex.Modules.Auth.Domain.Tokens;
using Lex.Modules.Auth.Domain.Users;
using Lex.SharedKernel;

namespace Lex.Modules.Auth.Application.Services;

public sealed class AuthenticationService
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthenticationService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuthUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthTokenPair>> LoginAsync(
        string emailAddress,
        string plainPassword,
        CancellationToken cancellationToken = default)
    {
        var emailResult = Email.Create(emailAddress);
        if (emailResult.IsFailure)
            return Result.Failure<AuthTokenPair>(
                Error.Validation("Login.InvalidEmail", "Invalid email format."));

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);

        if (user is null)
            return Result.Failure<AuthTokenPair>(
                Error.NotFound("Login.InvalidCredentials", "Invalid email or password."));

        if (!user.IsActive)
            return Result.Failure<AuthTokenPair>(
                Error.Conflict("Login.UserInactive", "User account is inactive."));

        if (!_passwordHasher.Verify(plainPassword, user.PasswordHash))
            return Result.Failure<AuthTokenPair>(
                Error.NotFound("Login.InvalidCredentials", "Invalid email or password."));

        var accessToken = _jwtTokenGenerator.Generate(user);

        var refreshTokenString = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var tokenHash = new TokenHash(refreshTokenString);

        var refreshToken = RefreshToken.Issue(
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.AddDays(7));

        _refreshTokenRepository.Add(refreshToken);

        // 🔥 KRİTİK EKSİK KISIM
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthTokenPair(
            accessToken,
            refreshTokenString,
            3600));
    }
}
