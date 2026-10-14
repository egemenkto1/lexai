using Lex.Modules.Auth.Application.Services;
using Lex.Modules.Auth.Domain.Repositories;
using Lex.Modules.Auth.Infrastructure.Data;
using Lex.Modules.Auth.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Lex.Modules.Auth.Infrastructure.Extensions;

public static class AuthDependencyInjection
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        // DbContext zaten host'ta register edilmiş

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Unit of Work
        services.AddScoped<IAuthUnitOfWork, AuthUnitOfWork>();

        // Application services
        services.AddScoped<UserRegistrationService>();
        services.AddScoped<AuthenticationService>();

        return services;
    }
}
