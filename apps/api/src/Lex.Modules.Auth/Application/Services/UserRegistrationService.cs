using Lex.Modules.Auth.Application.Contracts;
using Lex.Modules.Auth.Domain.Repositories;
using Lex.Modules.Auth.Domain.Users;
using Lex.SharedKernel;

namespace Lex.Modules.Auth.Application.Services;

public sealed class UserRegistrationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthUnitOfWork _unitOfWork;

    public UserRegistrationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserId>> RegisterAsync(
        string emailAddress,
        string plainPassword,
        string? fullName,
        CancellationToken cancellationToken = default)
    {
        var emailResult = Email.Create(emailAddress);
        if (emailResult.IsFailure)
            return Result.Failure<UserId>(emailResult.Error);

        var email = emailResult.Value;

        var exists = await _userRepository.ExistsByEmailAsync(email, cancellationToken);
        if (exists)
            return Result.Failure<UserId>(Error.Conflict("User.EmailExists", "This email is already registered."));

        var passwordHash = _passwordHasher.Hash(plainPassword);

        var user = User.Register(email, passwordHash, fullName);

        _userRepository.Add(user);

        // 🔥 KRİTİK EKLENTİ (Gemini’nin dediği yer)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Id);
    }
}
