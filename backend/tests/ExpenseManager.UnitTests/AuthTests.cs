using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Features.Auth;
using ExpenseManager.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ExpenseManager.UnitTests;

public class AuthTests : TestBase
{
    private readonly IPasswordHasher _hasher = new BcryptPasswordHasher();
    private readonly Mock<IJwtTokenService> _jwt = new();

    public AuthTests()
    {
        _jwt.Setup(x => x.GenerateAccessToken(It.IsAny<Domain.Entities.User>())).Returns("access-token");
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns(() => Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task Register_ShouldCreateUserWithDefaultCategoriesAndWallet()
    {
        var handler = new RegisterCommandHandler(Uow, _hasher, _jwt.Object, Mapper);

        var result = await handler.Handle(
            new RegisterCommand("new@example.com", "Người dùng mới", "secret123"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.User.Email.Should().Be("new@example.com");
        (await Context.Users.CountAsync()).Should().Be(1);
        (await Context.Categories.CountAsync()).Should().BeGreaterThan(5);
        (await Context.Wallets.CountAsync()).Should().Be(1);
        (await Context.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Register_ShouldThrowConflict_WhenEmailExists()
    {
        var handler = new RegisterCommandHandler(Uow, _hasher, _jwt.Object, Mapper);
        await handler.Handle(new RegisterCommand("dup@example.com", "A", "secret123"), CancellationToken.None);

        var act = () => handler.Handle(new RegisterCommand("DUP@example.com", "B", "secret456"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Login_ShouldSucceed_WithCorrectPassword()
    {
        var register = new RegisterCommandHandler(Uow, _hasher, _jwt.Object, Mapper);
        await register.Handle(new RegisterCommand("login@example.com", "A", "secret123"), CancellationToken.None);

        var handler = new LoginCommandHandler(Uow, _hasher, _jwt.Object, Mapper);
        var result = await handler.Handle(new LoginCommand("login@example.com", "secret123"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldThrowUnauthorized_WithWrongPassword()
    {
        var register = new RegisterCommandHandler(Uow, _hasher, _jwt.Object, Mapper);
        await register.Handle(new RegisterCommand("login2@example.com", "A", "secret123"), CancellationToken.None);

        var handler = new LoginCommandHandler(Uow, _hasher, _jwt.Object, Mapper);
        var act = () => handler.Handle(new LoginCommand("login2@example.com", "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task RefreshToken_ShouldRotateToken()
    {
        var register = new RegisterCommandHandler(Uow, _hasher, _jwt.Object, Mapper);
        var auth = await register.Handle(new RegisterCommand("rt@example.com", "A", "secret123"), CancellationToken.None);

        var handler = new RefreshTokenCommandHandler(Uow, _jwt.Object, Mapper);
        var result = await handler.Handle(new RefreshTokenCommand(auth.RefreshToken), CancellationToken.None);

        result.RefreshToken.Should().NotBe(auth.RefreshToken);

        // Token cũ đã bị thu hồi, dùng lại phải bị từ chối
        var act = () => handler.Handle(new RefreshTokenCommand(auth.RefreshToken), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
