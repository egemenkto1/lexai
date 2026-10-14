namespace Lex.Modules.Auth.Application.DTOs;

public record AuthTokenPair(string AccessToken, string RefreshToken, int ExpiresIn);
