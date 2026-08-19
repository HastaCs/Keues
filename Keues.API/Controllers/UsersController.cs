using System.Security.Claims;
using Keues.API.Common;
using Keues.API.Responses;
using Keues.API.Responses.Users;
using Keues.Application.Features.Users.CreateAdmin;
using Keues.Application.Features.Users.ForgotPassword;
using Keues.Application.Features.Users.HasAdmin;
using Keues.Application.Features.Users.Login;
using Keues.Application.Features.Users.Me;
using Keues.Application.Features.Users.ResetPassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Keues.API.Controllers
{
  /// <summary>
  /// Administrator authentication and retrieval of the current user.
  /// Authentication is performed using the HttpOnly "access_token" cookie (JWT).
  /// </summary>
  [Route("api/[controller]")]
  [ApiController]
  public class UsersController : ControllerBase
  {
    private readonly CreateAdminHandle _createAdminHandle;
    private readonly LoginHandler _loginHandler;
    private readonly HasAdminHandler _hasAdminHandler;
    private readonly GetCurrentUserHandler _getCurrentUserHandler;
    private readonly ForgotPasswordHandler _forgotPasswordHandler;
    private readonly ResetPasswordHandler _resetPasswordHandler;

    public UsersController(CreateAdminHandle createAdminHandle, LoginHandler loginHandler,
      HasAdminHandler hasAdminHandler, GetCurrentUserHandler getCurrentUserHandler,
      ForgotPasswordHandler forgotPasswordHandler, ResetPasswordHandler resetPasswordHandler)
    {
      _createAdminHandle = createAdminHandle;
      _loginHandler = loginHandler;
      _hasAdminHandler = hasAdminHandler;
      _getCurrentUserHandler = getCurrentUserHandler;
      _forgotPasswordHandler = forgotPasswordHandler;
      _resetPasswordHandler = resetPasswordHandler;
    }

    /// <summary>
    /// Creates the first administrator of the system. Returns the JWT in the body and sets it in the "access_token" cookie.
    /// </summary>
    /// <param name="request">Administrator name, email, and password.</param>
    /// <returns>The created administrator with its JWT.</returns>
    /// <response code="200">Administrator created.</response>
    /// <response code="400">Validation or business rule error.</response>
    [HttpPost("create-admin")]
    [ProducesResponseType(typeof(CreateAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdmin(CreateAdminCommand request)
    {
      try
      {
        var result = await _createAdminHandle.Handle(request);
        AppendAuthCookie.Append(Response, result.Jwt);
        return Ok(result);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Logs in an administrator. Returns the JWT in the body and sets it in the "access_token" cookie.
    /// </summary>
    /// <param name="request">Administrator email and password.</param>
    /// <returns>The session JWT.</returns>
    /// <response code="200">Successful login.</response>
    /// <response code="400">Invalid credentials or another error.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginCommand request)
    {
      try
      {
        var login = await _loginHandler.Handle(request);
       AppendAuthCookie.Append(Response, login.Jwt);
        return Ok(login);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Indicates whether an administrator has already been created in the system.
    /// </summary>
    /// <returns>true if at least one administrator already exists.</returns>
    /// <response code="200">Result of the check.</response>
    [HttpGet("has-admin")]
    [ProducesResponseType(typeof(HasAdminResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> HasAdmin()
    {
      var hasAdmin = await _hasAdminHandler.Handle(new HasAdminQuery());
      return Ok(new HasAdminResponse(hasAdmin));
    }

    /// <summary>
    /// Gets the data of the authenticated user. Requires the "access_token" cookie.
    /// </summary>
    /// <returns>Data of the current user.</returns>
    /// <response code="200">Current user.</response>
    /// <response code="400">Validation or business rule error.</response>
    /// <response code="401">Not authenticated.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
      try
      {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var me = await _getCurrentUserHandler.Handle(new MeQuery(userId));
        return Ok(me);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Requests a password reset. If the email exists, sends an email with a recovery link.
    /// Always responds 200 to avoid revealing whether the email is registered.
    /// </summary>
    /// <param name="request">Administrator email.</param>
    /// <response code="200">Request processed.</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand request)
    {
      try
      {
        await _forgotPasswordHandler.Handle(request);
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Resets the password using the token received by email.
    /// </summary>
    /// <param name="request">Token, email, and new password.</param>
    /// <response code="200">Password updated.</response>
    /// <response code="400">Invalid, expired token, or another error.</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand request)
    {
      try
      {
        await _resetPasswordHandler.Handle(request);
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Logs out by deleting the "access_token" cookie.
    /// </summary>
    /// <response code="200">Session closed.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
      Response.Cookies.Delete("access_token", new CookieOptions
      {
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Lax
      });
      return Ok();
    }

  }
}
