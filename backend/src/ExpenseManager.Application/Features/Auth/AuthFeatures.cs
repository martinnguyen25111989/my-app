using AutoMapper;
using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Auth;

// ===== Register =====
public record RegisterCommand(string Email, string FullName, string Password) : IRequest<AuthResponse>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public class RegisterCommandHandler(
    IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt, IMapper mapper)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await uow.Users.Query().AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Email đã được sử dụng.");

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = hasher.Hash(request.Password)
        };
        await uow.Users.AddAsync(user, ct);

        foreach (var c in DefaultData.Categories)
            await uow.Categories.AddAsync(new Category
            {
                Name = c.Name, Type = c.Type, Icon = c.Icon, Color = c.Color, UserId = user.Id
            }, ct);

        await uow.Wallets.AddAsync(new Wallet
        {
            Name = "Tiền mặt", Type = WalletType.Cash, InitialBalance = 0, UserId = user.Id
        }, ct);

        var refreshToken = new RefreshToken
        {
            Token = jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = user.Id
        };
        await uow.RefreshTokens.AddAsync(refreshToken, ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = jwt.GenerateAccessToken(user),
            RefreshToken = refreshToken.Token,
            User = mapper.Map<UserDto>(user)
        };
    }
}

// ===== Login =====
public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(
    IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt, IMapper mapper)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await uow.Users.Query().FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        var refreshToken = new RefreshToken
        {
            Token = jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = user.Id
        };
        await uow.RefreshTokens.AddAsync(refreshToken, ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = jwt.GenerateAccessToken(user),
            RefreshToken = refreshToken.Token,
            User = mapper.Map<UserDto>(user)
        };
    }
}

// ===== Refresh token (rotation) =====
public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandHandler(IUnitOfWork uow, IJwtTokenService jwt, IMapper mapper)
    : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var token = await uow.RefreshTokens.Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (token is null || !token.IsActive)
            throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn.");

        token.RevokedAt = DateTime.UtcNow;
        var newToken = new RefreshToken
        {
            Token = jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = token.UserId
        };
        await uow.RefreshTokens.AddAsync(newToken, ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = jwt.GenerateAccessToken(token.User),
            RefreshToken = newToken.Token,
            User = mapper.Map<UserDto>(token.User)
        };
    }
}

internal static class DefaultData
{
    public static readonly (string Name, TransactionType Type, string Icon, string Color)[] Categories =
    [
        ("Lương", TransactionType.Income, "payments", "#2e7d32"),
        ("Thưởng", TransactionType.Income, "card_giftcard", "#558b2f"),
        ("Thu nhập khác", TransactionType.Income, "savings", "#00897b"),
        ("Ăn uống", TransactionType.Expense, "restaurant", "#e53935"),
        ("Di chuyển", TransactionType.Expense, "directions_car", "#fb8c00"),
        ("Mua sắm", TransactionType.Expense, "shopping_cart", "#8e24aa"),
        ("Hóa đơn & Tiện ích", TransactionType.Expense, "receipt_long", "#3949ab"),
        ("Giải trí", TransactionType.Expense, "movie", "#d81b60"),
        ("Sức khỏe", TransactionType.Expense, "medical_services", "#00acc1"),
        ("Giáo dục", TransactionType.Expense, "school", "#6d4c41"),
        ("Chi tiêu khác", TransactionType.Expense, "more_horiz", "#757575")
    ];
}
