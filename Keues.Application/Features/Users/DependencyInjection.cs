using Keues.Application.Features.Users.CreateAdmin;
using Keues.Application.Features.Users.CreateUser;
using Keues.Application.Features.Users.ForgotPassword;
using Keues.Application.Features.Users.GetAllUsers;
using Keues.Application.Features.Users.GetUser;
using Keues.Application.Features.Users.HasAdmin;
using Keues.Application.Features.Users.Login;
using Keues.Application.Features.Users.Me;
using Keues.Application.Features.Users.ResetPassword;
using Keues.Application.Features.Users.UpdateUser;
using Microsoft.Extensions.DependencyInjection;

namespace Keues.Application.Features.Users;

public static class DependencyInjection
{
  public static IServiceCollection AddUsersUseCases(this IServiceCollection services)
  {
    services.AddScoped<CreateAdminHandle>();
    services.AddScoped<CreateUserHandler>();
    services.AddScoped<ForgotPasswordHandler>();
    services.AddScoped<HasAdminHandler>();
    services.AddScoped<LoginHandler>();
    services.AddScoped<GetCurrentUserHandler>();
    services.AddScoped<ResetPasswordHandler>();
    services.AddScoped<UpdateUserHandler>();
    services.AddScoped<GetUserHandler>();
    services.AddScoped<GetAllUsersHandler>();
    
    services.AddScoped<UsersUseCases>();
    return services;
  }
}