using Lex.Modules.Auth.Application.Contracts;
using Lex.Modules.Auth.Application.Services;
using Lex.Modules.Auth.Domain.Repositories;
using Lex.Modules.Auth.Infrastructure.Repositories;
using Lex.Modules.Auth.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Lex.Modules.Auth.Infrastructure.Extensions;

public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        // Repositories & UoW
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthUnitOfWork, AuthUnitOfWork>();

        // Services
        services.AddScoped<UserRegistrationService>();
        services.AddScoped<AuthenticationService>();

        // Infrastructure
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
