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

namespace Keues.Application.Features.Users;

public class UsersUseCases(CreateAdminHandle createAdmin, CreateUserHandler createUser, ForgotPasswordHandler forgotPassword, HasAdminHandler hasAdmin, LoginHandler login,
  GetCurrentUserHandler getCurrentUser, ResetPasswordHandler resetPassword, UpdateUserHandler updateUser, GetUserHandler getUser,
  GetAllUsersHandler getAllUsers,EnableDisableUserHandler enableDisableUser,DeleteUserHandler deleteUser)
{
  public CreateAdminHandle CreateAdmin => createAdmin;
  public CreateUserHandler CreateUser => createUser;
  public ForgotPasswordHandler ForgotPassword => forgotPassword;
  public HasAdminHandler HasAdmin => hasAdmin;
  public LoginHandler Login => login;
  public GetCurrentUserHandler GetCurrentUser => getCurrentUser;
  public ResetPasswordHandler ResetPassword => resetPassword;
  public UpdateUserHandler UpdateUser => updateUser;
  public GetUserHandler GetUser => getUser;
  
  public GetAllUsersHandler GetAllUsers => getAllUsers;
  
  public EnableDisableUserHandler EnableDisableUser => enableDisableUser;
  public DeleteUserHandler DeleteUser => deleteUser;
}