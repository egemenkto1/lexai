using Lex.Modules.Auth.Domain.Users;

namespace Lex.Modules.Auth.Application.Contracts;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
