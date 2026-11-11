using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Common;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Features.Users;

public static class UserEndpoints
{
    public record UserDto(Guid Id, string Name, string Email, string Role, bool Active, DateTime CreatedAt);
    public record CreateUserRequest(string Name, string Email, string Password, Role Role);
    public record UpdateUserRequest(string Name, Role Role, bool Active, string? Password);

    public class CreateValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(120).WithName("Nome");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(160).WithName("E-mail");
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithName("Senha");
            RuleFor(x => x.Role).IsInEnum().WithName("Perfil");
        }
    }

    public class UpdateValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(120).WithName("Nome");
            RuleFor(x => x.Role).IsInEnum().WithName("Perfil");
            RuleFor(x => x.Password).MinimumLength(6).When(x => !string.IsNullOrEmpty(x.Password)).WithName("Senha");
        }
    }

    public static void MapUsers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization(Policies.Admin);

        group.MapGet("/", List);
        group.MapPost("/", Create).Validate<CreateUserRequest>();
        group.MapPut("/{id:guid}", Update).Validate<UpdateUserRequest>();
    }

    private static async Task<PagedResult<UserDto>> List(AppDbContext db, string? search, Role? role,
        int? page, int? pageSize, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Name, term) || EF.Functions.ILike(u.Email, term));
        }

        if (role is not null)
            query = query.Where(u => u.Role == role);

        return await query
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.Active, u.CreatedAt))
            .ToPagedAsync(page, pageSize, ct);
    }

    private static async Task<IResult> Create(CreateUserRequest req, AppDbContext db, IPasswordHasher<User> hasher,
        CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Results.Problem(statusCode: 409, title: "Conflito", detail: "Já existe um usuário com este e-mail.");

        var user = new User { Name = req.Name.Trim(), Email = email, Role = req.Role };
        user.PasswordHash = hasher.HashPassword(user, req.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/users/{user.Id}",
            new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.Active, user.CreatedAt));
    }

    private static async Task<IResult> Update(Guid id, UpdateUserRequest req, AppDbContext db,
        IPasswordHasher<User> hasher, System.Security.Claims.ClaimsPrincipal principal, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([id], ct);
        if (user is null) return Results.NotFound();

        if (user.Id == principal.UserId() && (!req.Active || req.Role != Role.Admin))
            throw new DomainException("Você não pode desativar ou remover o perfil de administrador do próprio usuário.");

        user.Name = req.Name.Trim();
        user.Role = req.Role;
        user.Active = req.Active;
        if (!string.IsNullOrEmpty(req.Password))
            user.PasswordHash = hasher.HashPassword(user, req.Password);

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
