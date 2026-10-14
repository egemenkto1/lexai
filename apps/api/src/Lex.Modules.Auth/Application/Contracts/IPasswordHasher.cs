using Lex.Modules.Auth.Domain.Users;

namespace Lex.Modules.Auth.Application.Contracts;

public interface IPasswordHasher
{
    PasswordHash Hash(string password);
    bool Verify(string password, PasswordHash hash);
}
