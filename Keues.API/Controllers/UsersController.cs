using System.Security.Claims;
using Keues.API.Common;
using Keues.API.Dtos.Requests.Users;
using Keues.API.Mappers;
using Keues.API.Responses;
using Keues.API.Responses.Users;
using Keues.Application.Features.Users;
using Keues.Application.Features.Users.CreateAdmin;
using Keues.Application.Features.Users.CreateUser;
using Keues.Application.Features.Users.DeleteUser;
using Keues.Application.Features.Users.EnableDisableUser;
using Keues.Application.Features.Users.ForgotPassword;
using Keues.Application.Features.Users.GetAllUsers;
using Keues.Application.Features.Users.GetUser;
using Keues.Application.Features.Users.HasAdmin;
using Keues.Application.Features.Users.Login;
using Keues.Application.Features.Users.Me;
using Keues.Application.Features.Users.ResetPassword;
using Keues.Application.Features.Users.UpdateUser;
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
    private readonly UsersUseCases _userUseCases;

    public UsersController(UsersUseCases userUseCases)
    {
      _userUseCases = userUseCases;
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
        var result = await _userUseCases.CreateAdmin.Handle(request);
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
        var login = await _userUseCases.Login.Handle(request);
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
      var hasAdmin = await _userUseCases.HasAdmin.Handle(new HasAdminQuery());
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
        var me = await _userUseCases.GetCurrentUser.Handle(new MeQuery(userId));
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
        await _userUseCases.ForgotPassword.Handle(request);
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
        await _userUseCases.ResetPassword.Handle(request);
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


    /// <summary>
    /// Create a new User. Only accessible by Admins. Returns the created user.
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
      try
      {
        var command= request.ToCommand();
        var result = await _userUseCases.CreateUser.Handle(command);
        return Ok(result);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    /// <summary>
    /// Update a new User. Only accessible by Admins. Returns the updated user.
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
    {
      try
      {
        var command =request.ToCommand(id);
        var result = await _userUseCases.UpdateUser.Handle(command);
        return Ok(result);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
    
    /// <summary>
    /// Gets a user by its identifier. Only accessible by Admins. Returns the user.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUser(Guid id)
    {
      try
      {
        var query = new GetUserQuery(id);
        var result = await _userUseCases.GetUser.Handle(query);
        return Ok(result);
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
    
    
    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<IEnumerable<GetUserResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllUsers([FromQuery] GetAllUsersQuery query)
    {
      try
      {
        var result = await _userUseCases.GetAllUsers.Handle(query);
        var pagination = new Pagination(result.Page, result.Limit, result.Total, result.TotalPages);
        return Ok(new DataResponse<IEnumerable<GetUserResult>>(result.Users, pagination));
      
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
    
    
    /// <summary>
    /// Enable or disable a user. Only accessible by Admins. Returns the updated user.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnableDisableUser(Guid id, EnableDisableUserRequest request)
    {
      try
      {
       var command= new EnableDisableUserCommand(id, request.IsEnabled);
        await _userUseCases.EnableDisableUser.Handle(command);
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }

    
    
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
      try
      {
        var command = new DeleteUserCommand(id);
        await _userUseCases.DeleteUser.Handle(command);
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new ErrorResponse(e.Message));
      }
    }
  }
}