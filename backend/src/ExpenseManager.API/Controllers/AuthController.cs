using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterCommand command) =>
        Ok(await mediator.Send(command));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command) =>
        Ok(await mediator.Send(command));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenCommand command) =>
        Ok(await mediator.Send(command));
}
