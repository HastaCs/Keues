using Keues.Application.Features.UserGroups.CreateUserGroup;
using Keues.Application.Features.UserGroups.DeleteUserGroup;
using Keues.Application.Features.UserGroups.GetAllUserGroups;
using Keues.Application.Features.UserGroups.GetUserGroup;
using Keues.Application.Features.UserGroups.UpdateUserGroup;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.UserGroups;

public static class DependencyInjection
{
  public static IServiceCollection AddUserGroupsUseCases(this IServiceCollection services)
  {
    services.AddScoped<CreateUserGroupHandler>();
    services.AddScoped<UpdateUserGroupHandler>();
    services.AddScoped<DeleteUserGroupHandler>();
    services.AddScoped<GetUserGroupHandler>();
    services.AddScoped<GetAllUserGroupsHandler>();
    services.AddScoped<UserGroupsUseCases>();

    return services;
  }
  
}