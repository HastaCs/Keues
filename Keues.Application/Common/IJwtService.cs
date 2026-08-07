using Keues.Domain.Entities;

namespace Keues.Application.Common;


  public interface IJwtService
  {
    string Generate(User user);

    string GeneratePasswordResetToken(User user);

    Guid? ValidatePasswordResetToken(string token);
  }
