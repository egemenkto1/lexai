using Lex.Modules.Auth.Application.Contracts;
using Lex.Modules.Auth.Domain.Users;

namespace Lex.Modules.Auth.Infrastructure.Security;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    public PasswordHash Hash(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return new PasswordHash(hash);
    }

    public bool Verify(string password, PasswordHash hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash.Value);
    }
}
