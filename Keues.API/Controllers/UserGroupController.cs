using Keues.API.Dtos.Requests.UserGroups;
using Keues.API.Mappers;
using Keues.API.Responses;
using Keues.Application.Features.UserGroups;
using Keues.Application.Features.UserGroups.CreateUserGroup;
using Keues.Application.Features.UserGroups.DeleteUserGroup;
using Keues.Application.Features.UserGroups.GetAllUserGroups;
using Keues.Application.Features.UserGroups.GetUserGroup;
using Keues.Application.Features.UserGroups.UpdateUserGroup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Keues.API.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class UserGroupController : ControllerBase
  {
    private readonly UserGroupsUseCases _userGroupsUseCases;

    public UserGroupController(UserGroupsUseCases userGroupsUseCases)
    {
      _userGroupsUseCases = userGroupsUseCases;
    }

    /// <summary>
    /// Get all user groups
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(DataResponse<IEnumerable<UserGroupBaseResult>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUserGroups([FromQuery] GetAllUserGroupsQuery query)
    {
      try
      {
        var result = await _userGroupsUseCases.GetAllUserGroups.Handle(query);
        return Ok(new DataResponse<IEnumerable<UserGroupBaseResult>>(result.UserGroups));
      }
      catch (Exception e)
      {
        return BadRequest(new { error = e.Message });
      }
    }

    /// <summary>
    /// Get a user group by ID
    /// </summary>
    /// <param name="id">The ID of the user group</param>
    /// <returns>The user group with the specified ID</returns>
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserGroupBaseResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserGroupById(Guid id)
    {
      try
      {
        var result = await _userGroupsUseCases.GetUserGroup.Handle(new GetUserGroupQuery(id));
        return Ok(result);
      }
      catch (Exception e)
      {
        return BadRequest(new { error = e.Message });
      }
    }

    /// <summary>
    /// Create a new user group
    /// </summary>
    /// <param name="request">The user group information</param>
    /// <returns>The created user group</returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(UserGroupBaseResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUserGroup([FromBody] CreateUserGroupRequest request)
    {
      try
      {
        var command = request.ToCommand();
        var result = await _userGroupsUseCases.CreateUserGroup.Handle(command);
        return Ok(result);
      }
      catch (Exception e)
      {
        return BadRequest(new { error = e.Message });
      }
    }

    /// <summary>
    /// Update an existing user group
    /// </summary>
    /// <param name="id">The ID of the user group to update</param>
    /// <param name="request">The updated user group information</param>
    /// <returns>The updated user group</returns>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserGroupBaseResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUserGroup(Guid id, [FromBody] UpdateUserGroupRequest request)
    {
      try
      {
        var command=request.ToCommand(id);
        var result = await _userGroupsUseCases.UpdateUserGroup.Handle(command);
        return Ok(result);
      }
      catch (Exception e)
      {
        return BadRequest(new { error = e.Message });
      }
    }
    
    /// <summary>
    /// Delete a user group by ID
    /// </summary>
    /// <param name="id">The ID of the user group to delete</param>
    /// <returns>No content if the deletion is successful</returns>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUserGroup(Guid id)
    {
      try
      {
        await _userGroupsUseCases.DeleteUserGroup.Handle(new DeleteUserGroupCommand(id));
        return Ok();
      }
      catch (Exception e)
      {
        return BadRequest(new { error = e.Message });
      }
    }
  }
}