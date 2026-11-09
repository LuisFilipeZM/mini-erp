using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Common;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;
using System.Security.Claims;

namespace MiniErp.Api.Features.Auth;

public static class AuthEndpoints
{
    public record LoginRequest(string Email, string Password);
    public record UserInfo(Guid Id, string Name, string Email, string Role);
    public record LoginResponse(string Token, DateTime ExpiresAt, UserInfo User);

    public class LoginValidator : AbstractValidator<LoginRequest>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithName("E-mail");
            RuleFor(x => x.Password).NotEmpty().WithName("Senha");
        }
    }

    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", Login).Validate<LoginRequest>().AllowAnonymous();
        group.MapGet("/me", Me).RequireAuthorization();
    }

    private static async Task<IResult> Login(LoginRequest req, AppDbContext db, TokenService tokens,
        IPasswordHasher<User> hasher, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !user.Active ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        {
            return Results.Problem(statusCode: 401, title: "Credenciais inválidas", detail: "E-mail ou senha incorretos.");
        }

        var (token, expiresAt) = tokens.Create(user);
        return Results.Ok(new LoginResponse(token, expiresAt,
            new UserInfo(user.Id, user.Name, user.Email, user.Role.ToString())));
    }

    private static async Task<IResult> Me(ClaimsPrincipal principal, AppDbContext db, CancellationToken ct)
    {
        var id = principal.UserId();
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == id && u.Active)
            .Select(u => new UserInfo(u.Id, u.Name, u.Email, u.Role.ToString()))
            .FirstOrDefaultAsync(ct);

        return user is null ? Results.Unauthorized() : Results.Ok(user);
    }
}
