using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SusPayi.Api.Common;
using SusPayi.Domain.Accounts;
using SusPayi.Infrastructure.Persistence;

namespace SusPayi.Api.Features.Auth;

internal sealed record RegisterRequest(string Email, string Password, string DisplayName);

internal sealed record LoginRequest(string Email, string Password);

internal sealed record RefreshRequest(string RefreshToken);

internal static class AuthEndpoints
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/auth")
            .WithTags("Kimlik")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth);

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapPost("/refresh", Refresh);
        return api;
    }

    private static async Task<Results<SignInHttpResult, ValidationProblem, ProblemHttpResult>> Register(
        RegisterRequest request,
        SusPayiDbContext db,
        IPasswordHasher<Account> hasher,
        TimeProvider time,
        CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > Account.EmailMaxLength || !request.Email.Contains('@', StringComparison.Ordinal))
        {
            errors["email"] = ["Geçerli bir e-posta adresi gir."];
        }

        if (request.Password is null || request.Password.Length is < PasswordMinLength or > PasswordMaxLength)
        {
            errors["password"] = [$"Şifre {PasswordMinLength}–{PasswordMaxLength} karakter olmalı."];
        }

        if (request.DisplayName is null || !NameRules.PlayerName().IsMatch(request.DisplayName.Trim()))
        {
            errors["displayName"] = ["Ad 3–20 karakter olmalı; harf, rakam, nokta ve alt çizgi kullanılabilir."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var email = Account.NormalizeEmail(request.Email);
        var displayName = request.DisplayName!.Trim();
        if (await db.Accounts.AnyAsync(a => a.Email == email || a.DisplayName == displayName, ct))
        {
            return AccountTaken();
        }

        var account = Account.Create(email, displayName, time.GetUtcNow());
        account.SetPasswordHash(hasher.HashPassword(account, request.Password!));
        db.Accounts.Add(account);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Kontrol ile ekleme arasında aynı e-posta/ad başka istekle alındı.
            return AccountTaken();
        }

        return SignIn(account);
    }

    private static async Task<Results<SignInHttpResult, ProblemHttpResult>> Login(
        LoginRequest request,
        SusPayiDbContext db,
        IPasswordHasher<Account> hasher,
        CancellationToken ct)
    {
        var email = Account.NormalizeEmail(request.Email ?? string.Empty);
        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Email == email, ct);

        if (account is null)
        {
            // Hesap var mı yok mu, yanıt süresinden anlaşılmasın diye yine de hash doğrulaması yapılır.
            hasher.VerifyHashedPassword(DummyAccount, DummyHash, request.Password ?? string.Empty);
            return InvalidCredentials();
        }

        var verification = hasher.VerifyHashedPassword(account, account.PasswordHash, request.Password ?? string.Empty);
        if (verification == PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var tracked = await db.Accounts.FirstAsync(a => a.Id == account.Id, ct);
            tracked.SetPasswordHash(hasher.HashPassword(tracked, request.Password!));
            await db.SaveChangesAsync(ct);
        }

        return SignIn(account);
    }

    private static async Task<Results<SignInHttpResult, ChallengeHttpResult>> Refresh(
        RefreshRequest request,
        IOptionsMonitor<BearerTokenOptions> bearerOptions,
        SusPayiDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var protector = bearerOptions.Get(BearerTokenDefaults.AuthenticationScheme).RefreshTokenProtector;
        var ticket = protector.Unprotect(request.RefreshToken);

        if (ticket?.Properties.ExpiresUtc is not { } expiresAt
            || time.GetUtcNow() >= expiresAt
            || !Guid.TryParse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
        {
            return TypedResults.Challenge();
        }

        var account = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId, ct);
        return account is null ? TypedResults.Challenge() : SignIn(account);
    }

    private static SignInHttpResult SignIn(Account account)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.DisplayName),
            ],
            BearerTokenDefaults.AuthenticationScheme);

        // Bearer token işleyicisi yanıta erişim ve yenileme belirteçlerini yazar.
        return TypedResults.SignIn(new ClaimsPrincipal(identity), authenticationScheme: BearerTokenDefaults.AuthenticationScheme);
    }

    private static ProblemHttpResult AccountTaken() =>
        TypedResults.Problem(
            title: "account.taken",
            detail: "Bu e-posta ya da oyuncu adı kullanımda.",
            statusCode: StatusCodes.Status409Conflict,
            extensions: new Dictionary<string, object?> { ["code"] = "account.taken" });

    private static ProblemHttpResult InvalidCredentials() =>
        TypedResults.Problem(
            title: "auth.invalid_credentials",
            detail: "E-posta veya şifre hatalı.",
            statusCode: StatusCodes.Status401Unauthorized,
            extensions: new Dictionary<string, object?> { ["code"] = "auth.invalid_credentials" });

    private static readonly Account DummyAccount = Account.Create("dummy@suspayi.invalid", "dummy", DateTimeOffset.UnixEpoch);

    private static readonly string DummyHash = new PasswordHasher<Account>().HashPassword(DummyAccount, "sus-payi-dummy-password");
}
