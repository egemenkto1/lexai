using Lex.SharedKernel;
using Lex.Modules.Auth.Domain.Users.Events;

namespace Lex.Modules.Auth.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    public Email Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public string? FullName { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core için private constructor
    private User() { }

    private User(UserId id, Email email, PasswordHash passwordHash, string? fullName) 
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = UserRole.User;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static User Register(Email email, PasswordHash passwordHash, string? fullName)
    {
        var user = new User(new UserId(Guid.NewGuid()), email, passwordHash, fullName);
        
        user.AddDomainEvent(UserRegistered.Create(user.Id, user.Email));
        
        return user;
    }

    public void ChangePassword(PasswordHash newHash)
    {
        PasswordHash = newHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Result Deactivate()
    {
        if (Role == UserRole.Admin)
            return Result.Failure(Error.Conflict("User.CannotDeactivateAdmin", "Admin users cannot be deactivated."));

        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }
}
