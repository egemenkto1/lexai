using Lex.Modules.Auth.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Modules.Auth.Api;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async ([FromBody] RegisterRequest request, [FromServices] UserRegistrationService service, CancellationToken ct) =>
        {
            var result = await service.RegisterAsync(request.Email, request.Password, request.FullName, ct);
            return result.IsSuccess ? Results.Ok(new { UserId = result.Value.Value }) : Results.BadRequest(result.Error);
        });

        group.MapPost("/login", async ([FromBody] LoginRequest request, [FromServices] AuthenticationService service, CancellationToken ct) =>
        {
            var result = await service.LoginAsync(request.Email, request.Password, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }
}

public record RegisterRequest(string Email, string Password, string? FullName);
public record LoginRequest(string Email, string Password);
