using System.Security.Principal;
using Keues.Application.Common;
using Keues.Domain.Entities;
using Keues.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Keues.Application.Features.Users.CreateAdmin;

public class CreateAdminHandle
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    
    public CreateAdminHandle(IApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }
    
    public async Task<CreateAdminResponse> Handle(CreateAdminCommand request)
    {
        
       var existingAdmin = await _context.Users.FirstOrDefaultAsync(x => x.Role==Rol.Admin);
       if (existingAdmin != null)
           throw new Exception("Admin user already exists");

       var email = request.Email.ToLower();
       var password=BCrypt.Net.BCrypt.HashPassword(request.Password);

       var newUser = new User()
       {
           Name = request.Name,
           Email = email,
           Role = Rol.Admin,
           PasswordHash = password,
       };
       _context.Users.Add(newUser);
       await _context.SaveChangesAsync();
       
       
       var token = _jwtService.Generate(newUser);
       return new CreateAdminResponse(newUser.Id, newUser.Name, newUser.Email, token);

    }
}